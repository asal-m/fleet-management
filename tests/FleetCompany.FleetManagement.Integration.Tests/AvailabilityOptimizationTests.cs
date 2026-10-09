using System.Collections.Concurrent;
using System.Diagnostics;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using FleetCompany.FleetManagement.Infrastructure.Availability;

namespace FleetCompany.FleetManagement.Integration.Tests;
// Revision deltas and measurements must not overlap other tests' writes.
[CollectionDefinition("Availability measurements", DisableParallelization = true)]
public sealed class AvailabilityMeasurementCollection
{
}

[Collection("Availability measurements")]
public sealed class AvailabilityOptimizationTests(ITestOutputHelper output)
{
    [HttpDependenciesFact]
    public async Task Revision_changes_only_when_vehicle_availability_membership_changes()
    {
        await using var f = new OperationsAcceptanceTests.Scenario();
        async Task Check(string operation, long delta, Func<Task> act)
        {
            var before = await Revision();
            await act();
            var actual = await Revision() - before;
            output.WriteLine($"{operation}: revision delta={actual}, expected={delta}");
            Assert.Equal(delta, actual);
        }

        Guid vehicle = default, driver = default, mission = default;
        async Task Status(int value)
        {
            using var response = await f.Manager.PutAsJsonAsync($"/api/fleet/vehicles/{vehicle}/status", new { status = value });
            response.EnsureSuccessStatusCode();
        }

        await Check("register inactive vehicle", 0, async () => vehicle = await f.Vehicle(false, 1000));
        await Check("maintain inactive vehicle", 0, () => f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/start"));
        await Check("activate under maintenance", 0, () => Status(1));
        await Check("complete maintenance of active vehicle", 1, () => f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/complete"));
        await Check("repeat active status", 0, () => Status(1));
        await Check("maintain active vehicle", 1, () => f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/start"));
        await Check("deactivate under maintenance", 0, () => Status(2));
        await Check("complete maintenance of inactive vehicle", 0, () => f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/complete"));
        await Check("activate eligible vehicle", 1, () => Status(1));
        await Check("register active vehicle", 1, async () => await f.Vehicle(true, 1000));
        await Check("register driver", 0, async () => driver = await f.Driver(true, ["TRUCK"]));
        await Check("create draft", 0, async () => mission = await f.Mission(false));
        var time = DateTimeOffset.UtcNow.AddDays(7).ToString("O");
        await Check("schedule", 0, () => f.Change(f.Operator, $"/api/operations/missions/{mission}/schedule", new { scheduledTime = time }));
        await Check("repeat schedule", 0, () => f.Change(f.Operator, $"/api/operations/missions/{mission}/schedule", new { scheduledTime = time }));
        async Task Assign(Guid id)
        {
            using var response = await f.Assign(id, vehicle, driver);
            response.EnsureSuccessStatusCode();
        }

        await Check("assign", 1, () => Assign(mission));
        await Check("repeat assignment", 0, () => Assign(mission));
        await Check("start", 0, () => f.Change(f.Operator, $"/api/operations/missions/{mission}/start"));
        await Check("repeat start", 0, () => f.Change(f.Operator, $"/api/operations/missions/{mission}/start"));
        await Check("reject maintenance of reserved vehicle", 0, async () =>
        {
            using var response = await f.Manager.PostAsJsonAsync($"/api/fleet/vehicles/{vehicle}/maintenance/start", new { });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        });
        await Check("complete", 1, () => f.Change(f.Operator, $"/api/operations/missions/{mission}/complete"));
        await Check("repeat complete", 0, () => f.Change(f.Operator, $"/api/operations/missions/{mission}/complete"));
        var draft = await f.Mission(false);
        await Check("cancel draft", 0, () => f.Change(f.Operator, $"/api/operations/missions/{draft}/cancel"));
        var scheduled = await f.Mission();
        await Check("cancel scheduled", 0, () => f.Change(f.Operator, $"/api/operations/missions/{scheduled}/cancel"));
        var second = await f.Mission();
        await Check("assign second mission", 1, () => Assign(second));
        await Check("cancel assigned", 1, () => f.Change(f.Operator, $"/api/operations/missions/{second}/cancel"));
        await Check("repeat cancellation", 0, () => f.Change(f.Operator, $"/api/operations/missions/{second}/cancel"));
    }

    [HttpDependenciesFact]
    public async Task Driver_registration_progresses_while_vehicle_coordination_lock_is_held()
    {
        await using var f = new OperationsAcceptanceTests.Scenario();
        await f.Driver(true, ["TRUCK"]); // Warm the route and transaction middleware.
        await using var owner = await OpenDatabase();
        await using var transaction = await owner.BeginTransactionAsync();
        await HoldVehicleLock(owner, transaction, Guid.NewGuid());
        var requests = Task.WhenAll(Enumerable.Range(0, 8).Select(_ => f.Driver(true, ["TRUCK"])));
        try
        {
            Assert.Equal(8, (await requests.WaitAsync(TimeSpan.FromSeconds(3))).Distinct().Count());
        }
        finally
        {
            await transaction.RollbackAsync();
            await requests;
        }
    }

    [HttpDependenciesFact]
    public async Task Resource_lock_blocks_its_vehicle_but_allows_independent_vehicle_and_mission_writes()
    {
        await using var f = new OperationsAcceptanceTests.Scenario();
        var heldVehicle = await f.Vehicle(true, 1000);
        var freeVehicle = await f.Vehicle(true, 1000);
        var mission = await f.Mission(false);
        await using var owner = await OpenDatabase();
        await using var transaction = await owner.BeginTransactionAsync();
        await HoldVehicleLock(owner, transaction, heldVehicle);
        var blocked = f.Manager.PutAsJsonAsync($"/api/fleet/vehicles/{heldVehicle}/status", new { status = 2 });
        try
        {
            using var unrelated = await f.Manager.PutAsJsonAsync($"/api/fleet/vehicles/{freeVehicle}/status", new { status = 2 }).WaitAsync(TimeSpan.FromSeconds(5));
            unrelated.EnsureSuccessStatusCode();
            await f.Change(f.Operator, $"/api/operations/missions/{mission}/schedule", new { scheduledTime = DateTimeOffset.UtcNow.AddDays(1).ToString("O") }).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(blocked.IsCompleted);
        }
        finally
        {
            await transaction.RollbackAsync();
            using var released = await blocked.WaitAsync(TimeSpan.FromSeconds(5));
            released.EnsureSuccessStatusCode();
        }
    }

    private static async Task HoldVehicleLock(NpgsqlConnection owner, NpgsqlTransaction transaction, Guid vehicleId)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", owner, transaction);
        command.Parameters.AddWithValue("key", AvailabilityCoordinator.LockKey("vehicle", vehicleId));
        await command.ExecuteNonQueryAsync();
    }

    [HttpDependenciesFact]
    public async Task Measure_cache_roundtrips_and_unrelated_write_lock_wait()
    {
        var commands = new CommandCounter();
        await using var f = new OperationsAcceptanceTests.Scenario(new FleetTestHost(commands));
        await f.Vehicle(true, 1000);
        await f.Driver(true, ["TRUCK"]);
        var spans = new ConcurrentBag<(string Trace, string Source, string Name, double Milliseconds)>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "Npgsql" or "FleetCompany.FleetManagement.Business",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => spans.Add((activity.TraceId.ToString(), activity.Source.Name, activity.DisplayName, activity.Duration.TotalMilliseconds))
        };
        ActivitySource.AddActivityListener(listener);
        async Task<(int Commands, double Milliseconds)> Read()
        {
            var trace = ActivityTraceId.CreateRandom().ToString();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/fleet/vehicles/available?limit=199");
            request.Headers.Add("traceparent", $"00-{trace}-0123456789abcdef-01");
            var elapsed = Stopwatch.StartNew();
            commands.Begin();
            using var response = await f.Operator.SendAsync(request);
            response.EnsureSuccessStatusCode();
            await response.Content.ReadAsByteArrayAsync();
            elapsed.Stop();
            return (commands.End(), elapsed.Elapsed.TotalMilliseconds);
        }

        var cold = await Read();
        var hits = new List<(int Commands, double Milliseconds)>();
        for (var i = 0; i < 20; i++)
            hits.Add(await Read());
        var churn = new List<(int Commands, double Milliseconds)>();
        for (var i = 0; i < 10; i++)
        {
            await f.Driver(true, ["TRUCK"]);
            churn.Add(await Read());
        }

        await using var owner = await OpenDatabase();
        await using var transaction = await owner.BeginTransactionAsync();
        await HoldVehicleLock(owner, transaction, Guid.NewGuid());
        var elapsed = Stopwatch.StartNew();
        var requestDurations = new ConcurrentBag<double>();
        var requests = Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            var timer = Stopwatch.StartNew();
            await f.Driver(true, ["TRUCK"]);
            requestDurations.Add(timer.Elapsed.TotalMilliseconds);
        }));
        await Task.Delay(350);
        var completedBeforeRelease = requests.IsCompletedSuccessfully;
        await transaction.RollbackAsync();
        await requests;
        elapsed.Stop();
        var waits = spans.Where(span => span.Name == "Infrastructure.AvailabilityLock").Select(span => span.Milliseconds).Order().ToArray();
        object Stats(List<(int Commands, double Milliseconds)> rows) => new
        {
            requests = rows.Count,
            databaseCommands = rows.Sum(row => row.Commands),
            meanMilliseconds = rows.Average(row => row.Milliseconds),
            p95Milliseconds = rows.Select(row => row.Milliseconds).Order().ElementAt((int)Math.Ceiling(rows.Count * .95) - 1)
        };
        Assert.True(cold.Commands > 0);
        Assert.All(hits, row => Assert.True(row.Commands > 0));
        output.WriteLine("M4_BENCHMARK=" + JsonSerializer.Serialize(new { coldCommands = cold.Commands, coldMilliseconds = cold.Milliseconds, warm = Stats(hits), afterDriverRegistration = Stats(churn), heldLockMilliseconds = 350, eightDriverRequestsCompletedBeforeRelease = completedBeforeRelease, eightDriverRequestsTotalMilliseconds = elapsed.Elapsed.TotalMilliseconds, driverRequestMeanMilliseconds = requestDurations.Average(), driverRequestP95Milliseconds = requestDurations.Order().Last(), coordinationSpanCount = waits.Length, coordinationWaitP95Milliseconds = waits.Length == 0 ? 0 : waits[(int)Math.Ceiling(waits.Length * .95) - 1] }));
    }

    private static async Task<NpgsqlConnection> OpenDatabase()
    {
        var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION"));
        await connection.OpenAsync();
        return connection;
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        private int enabled, count;
        public void Begin()
        {
            Interlocked.Exchange(ref count, 0);
            Volatile.Write(ref enabled, 1);
        }

        public int End()
        {
            Volatile.Write(ref enabled, 0);
            return Volatile.Read(ref count);
        }

        private void Recorded()
        {
            if (Volatile.Read(ref enabled) == 1)
                Interlocked.Increment(ref count);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            Recorded();
            return ValueTask.FromResult(result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            Recorded();
            return ValueTask.FromResult(result);
        }

        public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        {
            Recorded();
            return ValueTask.FromResult(result);
        }
    }

    private static async Task<long> Revision()
    {
        await using var connection = await OpenDatabase();
        return (long)(await new NpgsqlCommand("SELECT version FROM fleet_availability_revision WHERE id=1", connection).ExecuteScalarAsync())!;
    }
}
