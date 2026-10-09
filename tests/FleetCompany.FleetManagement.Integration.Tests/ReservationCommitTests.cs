using Microsoft.EntityFrameworkCore;
using MPCore.Application.Results;
using MPCore.Domain.Events;
using FleetCompany.FleetManagement.Api.Rest;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using Npgsql;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class ReservationCommitTests
{
    [PostgreSqlFact]
    public async Task Database_commit_conflicts_are_classified_and_rollback_all_changes()
    {
        var vehicle = Guid.NewGuid();
        var driver = Guid.NewGuid();
        var winner = Assigned(vehicle, driver);
        var ids = new List<Guid>
        {
            winner.Id
        };
        try
        {
            await using (var db = Database())
            {
                db.Set<Mission>().Add(winner);
                await db.SaveChangesAsync();
            }

            foreach (var vehicleConflict in new[]
            {
                true,
                false
            }

            )
            {
                var loser = Assigned(vehicleConflict ? vehicle : Guid.NewGuid(), vehicleConflict ? Guid.NewGuid() : driver);
                var unrelated = Mission.Create(Guid.NewGuid(), MissionLocation.Create("Unrelated"), MissionLocation.Create("B"), RequiredCapacity.Create(100));
                ids.Add(loser.Id);
                ids.Add(unrelated.Id);
                await using (var db = Database())
                {
                    await using var transaction = await db.Database.BeginTransactionAsync();
                    db.Set<Mission>().AddRange(loser, unrelated);
                    var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
                    var failure = VehicleExceptionMapper.Classify(error);
                    Assert.NotNull(failure);
                    Assert.Equal(ErrorCategory.Conflict, failure.Category);
                    Assert.Equal(vehicleConflict ? "VEHICLE_ALREADY_RESERVED" : "DRIVER_ALREADY_RESERVED", failure.Identity.Code);
                    Assert.DoesNotContain("Password", failure.Message.Key, StringComparison.OrdinalIgnoreCase);
                    await transaction.RollbackAsync();
                }

                await using var reader = Database();
                Assert.False(await reader.Set<Mission>().AnyAsync(x => x.Id == loser.Id || x.Id == unrelated.Id));
                Assert.Equal(1, await reader.Set<Mission>().CountAsync(x => ids.Contains(x.Id)));
            }
        }
        finally
        {
            await using var cleanup = Database();
            await cleanup.Set<Mission>().Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        }
    }

    [Theory]
    [InlineData("other_index", PostgresErrorCodes.UniqueViolation)]
    [InlineData("ux_active_mission_vehicle", PostgresErrorCodes.CheckViolation)]
    public void Unknown_database_failures_are_not_disguised_as_reservation_conflicts(string constraint, string state) => Assert.Null(VehicleExceptionMapper.Classify(new PostgresException("private details", "ERROR", "ERROR", state, constraintName: constraint)));
    private static Mission Assigned(Guid vehicle, Guid driver)
    {
        var mission = Mission.Create(Guid.NewGuid(), MissionLocation.Create("A"), MissionLocation.Create("B"), RequiredCapacity.Create(100));
        var now = DateTimeOffset.UtcNow;
        mission.Schedule(now.AddDays(1), now);
        mission.Assign(vehicle, driver, new AssignmentEligibility(true, false, 1000, false, true, false, true));
        return mission;
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION")!).Options, TimeProvider.System, new NullAggregateEventSink());
}
