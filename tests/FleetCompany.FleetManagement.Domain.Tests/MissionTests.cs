using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using MPCore.Domain.Rules;
using Xunit;

namespace FleetCompany.FleetManagement.Domain.Tests;

public sealed class MissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid VehicleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid DriverId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    public static IEnumerable<object[]> Transitions()
    {
        foreach (var state in Enum.GetValues<MissionStatus>())
            foreach (var operation in new[] { "Schedule", "Assign", "Start", "Complete", "Cancel" })
                yield return [state, operation];
    }

    [Theory]
    [MemberData(nameof(Transitions))]
    public void Every_operation_checks_its_source_state_and_rejections_preserve_all_fields(MissionStatus state, string operation)
    {
        var mission = InState(state);
        var before = Snapshot(mission);
        var allowed = operation switch
        {
            "Schedule" => state is MissionStatus.Draft or MissionStatus.Scheduled,
            "Assign" => state is MissionStatus.Scheduled or MissionStatus.Assigned,
            "Start" => state is MissionStatus.Assigned or MissionStatus.InProgress,
            "Complete" => state is MissionStatus.InProgress or MissionStatus.Completed,
            "Cancel" => state is MissionStatus.Draft or MissionStatus.Scheduled or MissionStatus.Assigned or MissionStatus.Cancelled,
            _ => false
        };
        bool Act() => operation switch
        {
            "Schedule" => mission.Schedule(Now.AddHours(2), Now),
            "Assign" => mission.Assign(VehicleId, DriverId),
            "Start" => mission.Start(),
            "Complete" => mission.Complete(),
            "Cancel" => mission.Cancel(),
            _ => throw new InvalidOperationException()
        };
        if (!allowed)
        {
            Assert.Throws<BusinessRuleValidationException>(() => Act());
            Assert.Equal(before, Snapshot(mission));
            return;
        }
        var expected = operation switch
        {
            "Schedule" => MissionStatus.Scheduled,
            "Assign" => MissionStatus.Assigned,
            "Start" => MissionStatus.InProgress,
            "Complete" => MissionStatus.Completed,
            _ => MissionStatus.Cancelled
        };
        var changed = Act();
        Assert.Equal(expected, mission.Status);
        Assert.Equal(expected is MissionStatus.Assigned or MissionStatus.InProgress, mission.HasActiveReservation);
        Assert.Equal(operation == "Schedule" || state != expected, changed);
        if (!changed) Assert.Equal(before, Snapshot(mission));
    }

    [Fact]
    public void Full_lifecycle_keeps_resource_history_and_repeated_operations_have_no_new_effect()
    {
        var mission = Create();
        Assert.Null(mission.ScheduledTime);
        Assert.Null(mission.AssignedVehicleId);
        Assert.Null(mission.AssignedDriverId);
        Assert.True(mission.Schedule(Now.AddHours(1), Now));
        Assert.False(mission.Schedule(Now.AddHours(1), Now));
        Assert.True(mission.Assign(VehicleId, DriverId));
        Assert.False(mission.Assign(VehicleId, DriverId));
        Assert.True(mission.HasActiveReservation);
        Assert.True(mission.Start()); // Starting before scheduled time is permitted.
        Assert.False(mission.Start());
        Assert.True(mission.Complete());
        Assert.False(mission.Complete());
        Assert.False(mission.HasActiveReservation);
        Assert.Equal(VehicleId, mission.AssignedVehicleId);
        Assert.Equal(DriverId, mission.AssignedDriverId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Changing_either_assigned_resource_is_rejected_without_mutation(bool changeVehicle)
    {
        var mission = InState(MissionStatus.Assigned);
        var before = Snapshot(mission);
        Assert.Throws<BusinessRuleValidationException>(() => mission.Assign(
            changeVehicle ? Guid.NewGuid() : VehicleId, changeVehicle ? DriverId : Guid.NewGuid()));
        Assert.Equal(before, Snapshot(mission));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Empty_resource_ids_cannot_create_partial_assignment(bool emptyVehicle)
    {
        var mission = InState(MissionStatus.Scheduled);
        var before = Snapshot(mission);
        Assert.Throws<BusinessRuleValidationException>(() => mission.Assign(
            emptyVehicle ? Guid.Empty : VehicleId, emptyVehicle ? DriverId : Guid.Empty));
        Assert.Equal(before, Snapshot(mission));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Nonfuture_schedule_is_rejected_and_existing_schedule_survives(int minutes)
    {
        var mission = InState(MissionStatus.Scheduled);
        var before = Snapshot(mission);
        Assert.Throws<BusinessRuleValidationException>(() => mission.Schedule(Now.AddMinutes(minutes), Now));
        Assert.Equal(before, Snapshot(mission));
    }

    [Fact]
    public void Scheduling_compares_instants_and_stores_utc_without_reading_system_clock()
    {
        var mission = Create();
        var scheduled = Now.AddHours(1).ToOffset(TimeSpan.FromHours(3.5));
        mission.Schedule(scheduled, Now);
        Assert.Equal(TimeSpan.Zero, mission.ScheduledTime!.Value.Offset);
        Assert.Equal(Now.AddHours(1), mission.ScheduledTime);
        Assert.Throws<BusinessRuleValidationException>(() => mission.Schedule(
            Now.ToOffset(TimeSpan.FromHours(-5)), Now));
    }

    [Fact]
    public void Passing_the_scheduled_time_does_not_block_assignment_or_start()
    {
        var mission = Create();
        mission.Schedule(Now.AddHours(-1), Now.AddHours(-2));
        Assert.True(mission.Assign(VehicleId, DriverId));
        Assert.True(mission.Start());
    }

    [Fact]
    public void Equal_locations_are_valid_and_capacity_accepts_positive_boundaries()
    {
        var mission = Mission.Create(Guid.NewGuid(), MissionLocation.Create(" تهران "),
            MissionLocation.Create("تهران"), RequiredCapacity.Create(int.MaxValue));
        Assert.Equal(mission.Origin, mission.Destination);
        Assert.Equal(int.MaxValue, mission.RequiredCapacity.Kilograms);
        Assert.Equal(1, RequiredCapacity.Create(1).Kilograms);
        Assert.Equal(500, MissionLocation.Create(" " + new string('ن', 500) + " ").Value.Length);
        Assert.Throws<BusinessRuleValidationException>(() => MissionLocation.Create(new string('ن', 501)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_location_is_rejected(string? location)
        => Assert.Throws<BusinessRuleValidationException>(() => MissionLocation.Create(location));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Nonpositive_capacity_is_rejected(int capacity)
        => Assert.Throws<BusinessRuleValidationException>(() => RequiredCapacity.Create(capacity));

    private static Mission Create() => Mission.Create(Guid.NewGuid(), MissionLocation.Create("تهران"),
        MissionLocation.Create("شیراز"), RequiredCapacity.Create(1000));

    private static Mission InState(MissionStatus status)
    {
        var mission = Create();
        if (status == MissionStatus.Draft) return mission;
        mission.Schedule(Now.AddHours(1), Now);
        if (status == MissionStatus.Scheduled) return mission;
        mission.Assign(VehicleId, DriverId);
        if (status == MissionStatus.Assigned) return mission;
        if (status == MissionStatus.Cancelled) { mission.Cancel(); return mission; }
        mission.Start();
        if (status == MissionStatus.Completed) mission.Complete();
        return mission;
    }

    private static object Snapshot(Mission m) => (m.Status, m.ScheduledTime, m.AssignedVehicleId,
        m.AssignedDriverId, m.HasActiveReservation, m.Origin, m.Destination, m.RequiredCapacity);
}
