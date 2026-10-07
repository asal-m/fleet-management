using Microsoft.EntityFrameworkCore;
using Npgsql;
using MPCore.Domain.Events;
using MPCore.Persistence.EntityFrameworkCore.PostgreSql;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION")))
            Skip = "Run scripts/Start-LocalDependencies.ps1 -RunTests to test against the isolated Fleet database.";
    }
}

public sealed class VehiclePersistenceTests
{
    [PostgreSqlFact]
    public async Task Vehicle_round_trips_and_repository_reads_normalized_plate()
    {
        var token = Guid.NewGuid().ToString("N")[..20];
        var plate = PlateNumber.Create("TEST-" + token);
        var vehicle = Vehicle.Register(Guid.CreateVersion7(), plate, VehicleTypeCode.Create("TRUCK"),
            VehicleCapacity.Create(1000), VehicleBaseStatus.Active);
        try
        {
            await using (var database = CreateDatabase())
            {
                new VehicleRepository<AppDbContext>(database).Add(vehicle);
                await database.SaveChangesAsync();
            }

            await using var reader = CreateDatabase();
            var restored = await reader.Set<Vehicle>().SingleAsync(item => item.Id == vehicle.Id);
            Assert.Equal(plate, restored.PlateNumber);
            Assert.Equal(1000, restored.Capacity.Kilograms);
            Assert.False(restored.IsUnderMaintenance);
            Assert.True(await new VehicleRepository<AppDbContext>(reader).PlateExistsAsync(
                PlateNumber.Create(" test-" + token + " "), default));
        }
        finally
        {
            await using var cleanup = CreateDatabase();
            await cleanup.Set<Vehicle>().Where(item => item.PlateNumber == plate).ExecuteDeleteAsync();
        }
    }

    [PostgreSqlFact]
    public async Task Eight_concurrent_registrations_leave_exactly_one_plate()
    {
        var plate = PlateNumber.Create("RACE-" + Guid.NewGuid().ToString("N")[..20]);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = 0;
        async Task<bool> Attempt()
        {
            await using var database = CreateDatabase();
            var repository = new VehicleRepository<AppDbContext>(database);
            Assert.False(await repository.PlateExistsAsync(plate, default));
            repository.Add(Vehicle.Register(Guid.CreateVersion7(), plate, VehicleTypeCode.Create("TRUCK"),
                VehicleCapacity.Create(1000), VehicleBaseStatus.Active));
            if (Interlocked.Increment(ref waiting) == 8)
                ready.SetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            try
            {
                await database.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && postgres.ConstraintName == VehicleConfiguration.PlateUniqueIndex)
            {
                return false;
            }
        }

        try
        {
            var outcomes = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Attempt()));
            Assert.Equal(1, outcomes.Count(success => success));
            await using var reader = CreateDatabase();
            Assert.Equal(1, await reader.Set<Vehicle>().CountAsync(item => item.PlateNumber == plate));
        }
        finally
        {
            await using var cleanup = CreateDatabase();
            await cleanup.Set<Vehicle>().Where(item => item.PlateNumber == plate).ExecuteDeleteAsync();
        }
    }

    private static AppDbContext CreateDatabase()
    {
        var connection = Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("Integration database must be configured explicitly.");
        var options = new DbContextOptionsBuilder<AppDbContext>();
        PostgreSqlDbContextOptions.Apply(options, connection);
        // These tests verify EF/PostgreSQL, not Wolverine event delivery or audit middleware.
        return new AppDbContext(options.Options, TimeProvider.System, new NullAggregateEventSink());
    }
}
