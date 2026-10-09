using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MPCore.Audit;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class OperationsAcceptanceTests
{
    [HttpDependenciesFact]
    public async Task Commit_time_reservation_conflict_returns_409_and_rolls_back_success_audit()
    {
        // Deliberately stale read evidence forces the real unique index to be the final defence.
        await using var f = new Scenario(new FleetTestHost(configureServices: services => services.AddScoped<FleetCompany.FleetManagement.Modules.Operations.Contracts.IResourceReservations, StaleReservations>()));
        var vehicle = await f.Vehicle(true, 1000);
        var driver = await f.Driver(true, ["TRUCK"]);
        var winner = await f.Mission();
        using (var assigned = await f.Assign(winner, vehicle, driver))
            assigned.EnsureSuccessStatusCode();
        foreach (var vehicleConflict in new[]
        {
            true,
            false
        }

        )
        {
            var loser = await f.Mission();
            using var response = await f.Assign(loser, vehicleConflict ? vehicle : await f.Vehicle(true, 1000), vehicleConflict ? await f.Driver(true, ["TRUCK"]) : driver);
            await AssertFailure(response, HttpStatusCode.Conflict, "operations", vehicleConflict ? "VEHICLE_ALREADY_RESERVED" : "DRIVER_ALREADY_RESERVED");
            var state = await f.Read(loser);
            Assert.Equal(nameof(MissionStatus.Scheduled), state.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, state.GetProperty("assignedVehicleId").ValueKind);
            Assert.Equal(JsonValueKind.Null, state.GetProperty("assignedDriverId").ValueKind);
            Assert.DoesNotContain(await f.Audit(loser), x => x.GetProperty("action").GetString() == "MissionAssigned" && x.GetProperty("outcome").GetInt32() == (int)AuditOutcome.Succeeded);
        }
    }

    public sealed class StaleReservations : FleetCompany.FleetManagement.Modules.Operations.Contracts.IResourceReservations
    {
        public Task<bool> VehicleReservedAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> DriverReservedAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<IReadOnlyCollection<Guid>> ReservedVehiclesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Guid>>([]);
        public Task<IReadOnlyCollection<Guid>> ReservedDriversAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Guid>>([]);
    }

    [HttpDependenciesFact]
    public async Task Invalid_schedule_input_returns_validation_error_without_state_or_audit_changes()
    {
        await using var f = new Scenario();
        var mission = await f.Mission(scheduled: false);
        foreach (var time in new string?[]
        {
            null,
            "garbage",
            "2026-10-10T12:00:00"
        }

        )
        {
            using var response = await f.Operator.PostAsJsonAsync($"/api/operations/missions/{mission}/schedule", new { scheduledTime = time });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("SCHEDULED_TIME_INVALID", await response.Content.ReadAsStringAsync());
            var state = await f.Read(mission);
            Assert.Equal(nameof(MissionStatus.Draft), state.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, state.GetProperty("scheduledTime").ValueKind);
        }

        Assert.DoesNotContain(await f.Audit(mission), row => row.GetProperty("action").GetString() == "MissionScheduled");
    }

    [HttpDependenciesFact]
    public async Task Future_schedule_reserves_immediately_and_start_is_not_gated_by_scheduled_time()
    {
        await using var f = new Scenario();
        var vehicle = await f.Vehicle(true, 1000);
        var driver = await f.Driver(true, ["TRUCK"]);
        var futureTime = DateTimeOffset.UtcNow.AddDays(7);
        var future = await f.Mission(scheduledTime: futureTime);
        async Task<bool> Available(string path, Guid id)
        {
            using var response = await f.Operator.GetAsync(path + "?limit=200");
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.EnumerateArray().Any(row => row.GetProperty("id").GetGuid() == id);
        }

        Assert.True(await Available("/api/fleet/vehicles/available", vehicle));
        Assert.True(await Available("/api/drivers/available", driver));
        using (var assigned = await f.Assign(future, vehicle, driver))
            assigned.EnsureSuccessStatusCode();
        Assert.False(await Available("/api/fleet/vehicles/available", vehicle));
        Assert.False(await Available("/api/drivers/available", driver));
        var today = await f.Mission();
        using (var conflict = await f.Assign(today, vehicle, driver))
            await AssertFailure(conflict, HttpStatusCode.Conflict, "operations", "VEHICLE_ALREADY_RESERVED");
        await f.Change(f.Operator, $"/api/operations/missions/{future}/start");
        var state = await f.Read(future);
        Assert.Equal(nameof(MissionStatus.InProgress), state.GetProperty("status").GetString());
        Assert.True(state.GetProperty("scheduledTime").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddDays(6));
        Assert.False(await Available("/api/fleet/vehicles/available", vehicle));
        await f.Change(f.Operator, $"/api/operations/missions/{future}/complete");
        Assert.True(await Available("/api/fleet/vehicles/available", vehicle));
        Assert.True(await Available("/api/drivers/available", driver));
        using (var assigned = await f.Assign(today, vehicle, driver))
            assigned.EnsureSuccessStatusCode();
    }

    [HttpDependenciesFact]
    public async Task Missing_assignment_resources_are_not_found_and_keep_rejected_audit_and_state()
    {
        await using var f = new Scenario();
        var vehicle = await f.Vehicle(true, 1000);
        var driver = await f.Driver(true, ["TRUCK"]);
        foreach (var missingVehicle in new[]
        {
            true,
            false
        }

        )
        {
            var mission = await f.Mission();
            var code = missingVehicle ? "VEHICLE_NOT_FOUND" : "DRIVER_NOT_FOUND";
            using var response = await f.Assign(mission, missingVehicle ? Guid.NewGuid() : vehicle, missingVehicle ? driver : Guid.NewGuid());
            await AssertFailure(response, HttpStatusCode.NotFound, "operations", code);
            var state = await f.Read(mission);
            Assert.Equal(nameof(MissionStatus.Scheduled), state.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, state.GetProperty("assignedVehicleId").ValueKind);
            Assert.Equal(JsonValueKind.Null, state.GetProperty("assignedDriverId").ValueKind);
            var attempt = Assert.Single(await f.Audit(mission), x => x.GetProperty("action").GetString() == "MissionAssigned");
            Assert.Equal((int)AuditOutcome.Rejected, attempt.GetProperty("outcome").GetInt32());
        }
    }

    [HttpDependenciesFact]
    public async Task Maintenance_and_past_schedule_rules_are_unprocessable_and_preserve_state()
    {
        await using var f = new Scenario();
        var vehicle = await f.Vehicle(true, 1000);
        using (var notStarted = await f.Manager.PostAsJsonAsync($"/api/fleet/vehicles/{vehicle}/maintenance/complete", new { }))
            await AssertFailure(notStarted, HttpStatusCode.UnprocessableEntity, "fleet", "MAINTENANCE_NOT_STARTED");
        await f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/start");
        using (var repeated = await f.Manager.PostAsJsonAsync($"/api/fleet/vehicles/{vehicle}/maintenance/start", new { }))
            await AssertFailure(repeated, HttpStatusCode.UnprocessableEntity, "fleet", "MAINTENANCE_ALREADY_STARTED");
        using (var response = await f.Manager.GetAsync($"/api/fleet/vehicles/{vehicle}"))
        {
            response.EnsureSuccessStatusCode();
            using var state = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(state.RootElement.GetProperty("isUnderMaintenance").GetBoolean());
        }

        var audit = await f.Audit(vehicle);
        Assert.Equal(2, audit.Count(x => x.GetProperty("outcome").GetInt32() == (int)AuditOutcome.Rejected));
        Assert.Single(audit, x => x.GetProperty("action").GetString() == "MaintenanceStarted" && x.GetProperty("outcome").GetInt32() == (int)AuditOutcome.Succeeded);
        var mission = await f.Mission(false);
        using (var response = await f.Operator.PostAsJsonAsync($"/api/operations/missions/{mission}/schedule", new { scheduledTime = DateTimeOffset.UtcNow.AddHours(-1).ToString("O") }))
            await AssertFailure(response, HttpStatusCode.UnprocessableEntity, "operations", "SCHEDULED_TIME_NOT_FUTURE");
        var missionState = await f.Read(mission);
        Assert.Equal(nameof(MissionStatus.Draft), missionState.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, missionState.GetProperty("scheduledTime").ValueKind);
        Assert.Single(await f.Audit(mission), x => x.GetProperty("action").GetString() == "MissionScheduled" && x.GetProperty("outcome").GetInt32() == (int)AuditOutcome.Rejected);
    }

    private static async Task AssertFailure(HttpResponseMessage response, HttpStatusCode status, string domain, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(domain, problem.RootElement.GetProperty("errorDomain").GetString());
        Assert.Equal(code, problem.RootElement.GetProperty("errorCode").GetString());
    }

    [HttpDependenciesFact]
    public async Task Assignment_rules_audit_and_lifecycle_are_enforced_through_real_transactions()
    {
        await using var fixture = new Scenario();
        foreach (var code in new[]
        {
            "VEHICLE_INACTIVE",
            "VEHICLE_UNDER_MAINTENANCE",
            "DRIVER_INACTIVE",
            "DRIVER_UNQUALIFIED",
            "INSUFFICIENT_VEHICLE_CAPACITY"
        }

        )
        {
            var vehicle = await fixture.Vehicle(code != "VEHICLE_INACTIVE", code == "INSUFFICIENT_VEHICLE_CAPACITY" ? 999 : 1000);
            var driver = await fixture.Driver(code != "DRIVER_INACTIVE", code == "DRIVER_UNQUALIFIED" ? ["BUS"] : ["TRUCK"]);
            if (code == "VEHICLE_UNDER_MAINTENANCE")
                await fixture.Change(fixture.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/start");
            var mission = await fixture.Mission();
            using var rejected = await fixture.Assign(mission, vehicle, driver);
            await AssertFailure(rejected, HttpStatusCode.UnprocessableEntity, "operations", code);
            var state = await fixture.Read(mission);
            Assert.Equal(nameof(MissionStatus.Scheduled), state.GetProperty("status").GetString());
            Assert.Equal(JsonValueKind.Null, state.GetProperty("assignedVehicleId").ValueKind);
            var entries = await fixture.Audit(mission);
            var attempt = entries.Single(x => x.GetProperty("action").GetString() == "MissionAssigned");
            Assert.Equal((int)AuditOutcome.Rejected, attempt.GetProperty("outcome").GetInt32());
            Assert.Equal("fleet-http-test-user", attempt.GetProperty("actor").GetProperty("subjectId").GetString());
        }

        var availableVehicle = await fixture.Vehicle(true, 1000);
        var activeDriver = await fixture.Driver(true, ["TRUCK"]);
        var draft = await fixture.Mission(false);
        using (var invalid = await fixture.Assign(draft, availableVehicle, activeDriver))
            await AssertFailure(invalid, HttpStatusCode.UnprocessableEntity, "operations", "MISSION_STATE_INVALID");
        var first = await fixture.Mission();
        using (var assigned = await fixture.Assign(first, availableVehicle, activeDriver))
            assigned.EnsureSuccessStatusCode();
        using (var repeated = await fixture.Assign(first, availableVehicle, activeDriver))
            repeated.EnsureSuccessStatusCode();
        var other = await fixture.Mission();
        using (var conflict = await fixture.Assign(other, availableVehicle, await fixture.Driver(true, ["TRUCK"])))
        {
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Contains("VEHICLE_ALREADY_RESERVED", await conflict.Content.ReadAsStringAsync());
        }

        using (var conflict = await fixture.Assign(other, await fixture.Vehicle(true, 1000), activeDriver))
        {
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Contains("DRIVER_ALREADY_RESERVED", await conflict.Content.ReadAsStringAsync());
        }

        using (var maintenance = await fixture.Manager.PostAsJsonAsync($"/api/fleet/vehicles/{availableVehicle}/maintenance/start", new { }))
            Assert.Equal(HttpStatusCode.Conflict, maintenance.StatusCode);
        using (var inactive = await fixture.Manager.PutAsJsonAsync($"/api/fleet/vehicles/{availableVehicle}/status", new { status = 2 }))
            Assert.Equal(HttpStatusCode.Conflict, inactive.StatusCode);
        await fixture.Change(fixture.Operator, $"/api/operations/missions/{first}/start");
        await fixture.Change(fixture.Operator, $"/api/operations/missions/{first}/start");
        using (var cancelInProgress = await fixture.Operator.PostAsJsonAsync($"/api/operations/missions/{first}/cancel", new { }))
            await AssertFailure(cancelInProgress, HttpStatusCode.UnprocessableEntity, "operations", "MISSION_STATE_INVALID");
        await fixture.Change(fixture.Operator, $"/api/operations/missions/{first}/complete");
        await fixture.Change(fixture.Operator, $"/api/operations/missions/{first}/complete");
        using (var reassignedFinal = await fixture.Assign(first, availableVehicle, activeDriver))
            await AssertFailure(reassignedFinal, HttpStatusCode.UnprocessableEntity, "operations", "MISSION_STATE_INVALID");
        var successAudit = await fixture.Audit(first);
        foreach (var action in new[]
        {
            "MissionCreated",
            "MissionAssigned",
            "MissionStarted",
            "MissionCompleted"
        }

        )
            Assert.Single(successAudit, x => x.GetProperty("action").GetString() == action && x.GetProperty("outcome").GetInt32() == (int)AuditOutcome.Succeeded);
        using (var denied = await fixture.Operator.GetAsync($"/api/administration/audit/?entityId={first}"))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using (var assignedAgain = await fixture.Assign(other, availableVehicle, activeDriver))
            assignedAgain.EnsureSuccessStatusCode();
        await fixture.Change(fixture.Operator, $"/api/operations/missions/{other}/cancel");
        await fixture.Change(fixture.Operator, $"/api/operations/missions/{other}/cancel");
        Assert.Single(await fixture.Audit(other), x => x.GetProperty("action").GetString() == "MissionCancelled");
    }

    [HttpDependenciesFact]
    public async Task Eight_competing_assignments_commit_one_owner_and_do_not_partially_reserve_losers()
    {
        await using var f = new Scenario();
        var vehicle = await f.Vehicle(true, 1000);
        var driver = await f.Driver(true, ["TRUCK"]);
        var missions = new List<Guid>();
        for (var i = 0; i < 8; i++)
            missions.Add(await f.Mission());
        var responses = await Task.WhenAll(missions.Select(id => f.Assign(id, vehicle, driver)));
        try
        {
            Assert.Equal(1, responses.Count(x => x.IsSuccessStatusCode));
            Assert.Equal(7, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));
            var states = await Task.WhenAll(missions.Select(f.Read));
            Assert.Equal(1, states.Count(x => x.GetProperty("status").GetString() == nameof(MissionStatus.Assigned)));
            Assert.Equal(7, states.Count(x => x.GetProperty("assignedVehicleId").ValueKind == JsonValueKind.Null));
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    [HttpDependenciesFact]
    public async Task Cache_invalidation_tracks_assignment_completion_status_and_maintenance_across_hosts()
    {
        await using var f = new Scenario();
        var vehicle = await f.Vehicle(true, 1000);
        var driver = await f.Driver(true, ["TRUCK"]);
        await using var otherHost = new FleetTestHost();
        using var otherClient = otherHost.CreateClient();
        otherClient.DefaultRequestHeaders.Authorization = new("Bearer", otherHost.Token("Operator"));
        async Task<bool> Available(HttpClient client)
        {
            using var response = await client.GetAsync("/api/fleet/vehicles/available?limit=200");
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == vehicle);
        }

        Assert.True(await Available(f.Operator));
        Assert.True(await Available(otherClient));
        var mission = await f.Mission();
        using (var assigned = await f.Assign(mission, vehicle, driver))
            assigned.EnsureSuccessStatusCode();
        Assert.False(await Available(f.Operator));
        Assert.False(await Available(otherClient));
        using (var drivers = await f.Operator.GetAsync("/api/drivers/available?limit=200"))
        {
            drivers.EnsureSuccessStatusCode();
            Assert.DoesNotContain(driver.ToString(), await drivers.Content.ReadAsStringAsync());
        }

        await f.Change(f.Operator, $"/api/operations/missions/{mission}/start");
        await f.Change(f.Operator, $"/api/operations/missions/{mission}/complete");
        Assert.True(await Available(otherClient));
        using (var status = await f.Manager.PutAsJsonAsync($"/api/fleet/vehicles/{vehicle}/status", new { status = 2 }))
            status.EnsureSuccessStatusCode();
        Assert.False(await Available(otherClient));
        using (var status = await f.Manager.PutAsJsonAsync($"/api/fleet/vehicles/{vehicle}/status", new { status = 1 }))
            status.EnsureSuccessStatusCode();
        Assert.True(await Available(otherClient));
        await f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/start");
        Assert.False(await Available(otherClient));
        await f.Change(f.Manager, $"/api/fleet/vehicles/{vehicle}/maintenance/complete");
        Assert.True(await Available(otherClient));
        var audits = await f.Audit(vehicle);
        foreach (var action in new[]
        {
            "VehicleStatusChanged",
            "MaintenanceStarted",
            "MaintenanceCompleted"
        }

        )
            Assert.Contains(audits, x => x.GetProperty("action").GetString() == action);
    }

    [HttpDependenciesFact]
    public async Task Assignment_and_maintenance_cannot_both_win_the_same_vehicle()
    {
        await using var f = new Scenario();
        var vehicle = await f.Vehicle(true, 1000);
        var driver = await f.Driver(true, ["TRUCK"]);
        var mission = await f.Mission();
        var assignmentTask = f.Assign(mission, vehicle, driver);
        var maintenanceTask = f.Manager.PostAsJsonAsync($"/api/fleet/vehicles/{vehicle}/maintenance/start", new { });
        using var assigned = await assignmentTask;
        using var maintained = await maintenanceTask;
        Assert.NotEqual(assigned.IsSuccessStatusCode, maintained.IsSuccessStatusCode);
        // Existing reservations conflict; an under-maintenance vehicle is ineligible.
        Assert.Equal(assigned.IsSuccessStatusCode ? HttpStatusCode.Conflict : HttpStatusCode.UnprocessableEntity, assigned.IsSuccessStatusCode ? maintained.StatusCode : assigned.StatusCode);
        var state = await f.Read(mission);
        Assert.Equal((assigned.IsSuccessStatusCode ? MissionStatus.Assigned : MissionStatus.Scheduled).ToString(), state.GetProperty("status").GetString());
    }

    [HttpDependenciesFact]
    public async Task One_request_has_correlated_transport_application_domain_database_and_cache_spans()
    {
        await using var f = new Scenario();
        var mission = await f.Mission(false);
        var spans = new System.Collections.Concurrent.ConcurrentBag<(string Name, string Source, string Trace)>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name is "FleetCompany.FleetManagement.Business" or "Npgsql",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = span => spans.Add((span.DisplayName, span.Source.Name, span.TraceId.ToString()))
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        const string trace = "0123456789abcdef0123456789abcdef";
        f.Operator.DefaultRequestHeaders.Add("traceparent", $"00-{trace}-0123456789abcdef-01");
        using var scheduled = await f.Operator.PostAsJsonAsync($"/api/operations/missions/{mission}/schedule", new { scheduledTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O") });
        scheduled.EnsureSuccessStatusCode();
        Assert.Equal(trace, scheduled.Headers.GetValues("X-Trace-Id").Single());
        foreach (var name in new[]
        {
            "Transport.POST /api/operations/missions/{id:guid}/schedule",
            "Application.MissionScheduled",
            "Domain.Mission.Schedule",
            "Infrastructure.AvailabilityLock"
        }

        )
            Assert.Contains(spans, span => span.Name == name && span.Trace == trace);
        Assert.Contains(spans, span => span.Source == "Npgsql" && span.Trace == trace);
        using var cached = await f.Operator.GetAsync("/api/fleet/vehicles/available?limit=200");
        cached.EnsureSuccessStatusCode();
        Assert.Contains(spans, span => span.Name == "Infrastructure.HybridCache" && span.Trace == trace);
        using var invalidAudit = await f.Administrator.GetAsync("/api/administration/audit/?page=0&size=201");
        Assert.Equal(HttpStatusCode.BadRequest, invalidAudit.StatusCode);
    }

    internal sealed class Scenario : IAsyncDisposable
    {
        public FleetTestHost Host { get; }
        public HttpClient Manager { get; }
        public HttpClient Operator { get; }
        public HttpClient Administrator { get; }

        private readonly List<Guid> vehicles = [], drivers = [], missions = [];
        public Scenario(FleetTestHost? host = null)
        {
            Host = host ?? new();
            Manager = Client("FleetManager");
            Operator = Client("Operator");
            Administrator = Client("Administrator");
        }

        private HttpClient Client(string role)
        {
            var c = Host.CreateClient();
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Host.Token(role));
            return c;
        }

        private static async Task<Guid> Created(HttpResponseMessage response)
        {
            using (response)
            {
                response.EnsureSuccessStatusCode();
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return body.RootElement.GetProperty("id").GetGuid();
            }
        }

        public async Task<Guid> Vehicle(bool active, int capacity)
        {
            var id = await Created(await Manager.PostAsJsonAsync("/api/fleet/vehicles/", new { plateNumber = "ACC-" + Guid.NewGuid().ToString("N")[..20], typeCode = "TRUCK", capacityKilograms = capacity, baseStatus = active ? 1 : 2 }));
            vehicles.Add(id);
            return id;
        }

        public async Task<Guid> Driver(bool active, string[] qualifications)
        {
            var id = await Created(await Manager.PostAsJsonAsync("/api/drivers/", new { firstName = "Acceptance", lastName = "Driver", status = active ? 1 : 2, qualifications }));
            lock (drivers)
                drivers.Add(id);
            return id;
        }

        public async Task<Guid> Mission(bool scheduled = true, DateTimeOffset? scheduledTime = null)
        {
            var id = await Created(await Operator.PostAsJsonAsync("/api/operations/missions/", new { origin = "A", destination = "B", requiredCapacityKilograms = 1000 }));
            missions.Add(id);
            if (scheduled)
                await Change(Operator, $"/api/operations/missions/{id}/schedule", new { scheduledTime = (scheduledTime ?? DateTimeOffset.UtcNow.AddHours(1)).ToString("O") });
            return id;
        }

        public Task<HttpResponseMessage> Assign(Guid mission, Guid vehicle, Guid driver) => Operator.PostAsJsonAsync($"/api/operations/missions/{mission}/assign", new { vehicleId = vehicle, driverId = driver });
        public async Task Change(HttpClient client, string path, object? body = null)
        {
            using var response = await client.PostAsJsonAsync(path, body ?? new { });
            response.EnsureSuccessStatusCode();
        }

        public async Task<JsonElement> Read(Guid id)
        {
            using var response = await Operator.GetAsync($"/api/operations/missions/{id}");
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.Clone();
        }

        public async Task<JsonElement[]> Audit(Guid id)
        {
            using var response = await Administrator.GetAsync($"/api/administration/audit/?entityId={id}&size=200");
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.GetProperty("items").EnumerateArray().Select(x => x.Clone()).ToArray();
        }

        public async ValueTask DisposeAsync()
        {
            using var scope = Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Set<Mission>().Where(x => missions.Contains(x.Id)).ExecuteDeleteAsync();
            await db.Set<Vehicle>().Where(x => vehicles.Contains(x.Id)).ExecuteDeleteAsync();
            await db.Set<Driver>().Where(x => drivers.Contains(x.Id)).ExecuteDeleteAsync();
            Manager.Dispose();
            Operator.Dispose();
            Administrator.Dispose();
            await Host.DisposeAsync();
        }
    }
}
