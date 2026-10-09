using Microsoft.EntityFrameworkCore;
using MPCore.Domain.Events;
using MPCore.Persistence.EntityFrameworkCore.PostgreSql;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class MissionReadModelTests
{
    [PostgreSqlFact]
    public async Task Every_state_projects_nullable_time_and_resource_history_without_tracking_or_mutation()
    {
        var now = new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
        // Test fixtures exercise Domain/EF only; they are not the future assignment use case.
        var missions = Enum.GetValues<MissionStatus>().Select(status => Build(status, now)).ToArray();
        var ids = missions.Select(x => x.Id).ToArray();
        try
        {
            await using (var writer = CreateDatabase())
            {
                writer.Set<Mission>().AddRange(missions);
                await writer.SaveChangesAsync();
            }

            await using var reader = CreateDatabase();
            var readModel = new MissionReadModel<AppDbContext>(reader);
            foreach (var expected in missions)
            {
                var view = await readModel.GetAsync(expected.Id, default);
                Assert.NotNull(view);
                Assert.Equal(expected.Id, view.Id);
                Assert.Equal(expected.Origin.Value, view.Origin);
                Assert.Equal(expected.Destination.Value, view.Destination);
                Assert.Equal(expected.RequiredCapacity.Kilograms, view.RequiredCapacityKilograms);
                Assert.Equal(expected.Status, view.Status);
                Assert.Equal(expected.ScheduledTime, view.ScheduledTime);
                Assert.Equal(expected.AssignedVehicleId, view.AssignedVehicleId);
                Assert.Equal(expected.AssignedDriverId, view.AssignedDriverId);
                if (view.ScheduledTime.HasValue)
                    Assert.Equal(TimeSpan.Zero, view.ScheduledTime.Value.Offset);
            }

            Assert.Null(await readModel.GetAsync(Guid.NewGuid(), default));
            Assert.Empty(reader.ChangeTracker.Entries());
            Assert.Equal(6, await reader.Set<Mission>().CountAsync(x => ids.Contains(x.Id)));
        }
        finally
        {
            await using var cleanup = CreateDatabase();
            await cleanup.Set<Mission>().Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        }
    }

    private static Mission Build(MissionStatus status, DateTimeOffset now)
    {
        var mission = Mission.Create(Guid.CreateVersion7(), MissionLocation.Create("تهران"), MissionLocation.Create("شیراز"), RequiredCapacity.Create(1000));
        if (status == MissionStatus.Draft)
            return mission;
        mission.Schedule(now.AddHours(1).ToOffset(TimeSpan.FromHours(3.5)), now);
        if (status == MissionStatus.Scheduled)
            return mission;
        mission.Assign(Guid.NewGuid(), Guid.NewGuid());
        if (status == MissionStatus.Assigned)
            return mission;
        if (status == MissionStatus.Cancelled)
        {
            mission.Cancel();
            return mission;
        }

        mission.Start();
        if (status == MissionStatus.Completed)
            mission.Complete();
        return mission;
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        PostgreSqlDbContextOptions.Apply(options, Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION") ?? throw new InvalidOperationException("Integration database must be configured explicitly."));
        return new AppDbContext(options.Options, TimeProvider.System, new NullAggregateEventSink());
    }
}
