using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using MPCore.Domain.Rules;
using Xunit;

namespace FleetCompany.FleetManagement.Domain.Tests;

public sealed class AssignmentEligibilityTests
{
    [Theory]
    [InlineData("VEHICLE_INACTIVE")]
    [InlineData("VEHICLE_UNDER_MAINTENANCE")]
    [InlineData("INSUFFICIENT_VEHICLE_CAPACITY")]
    [InlineData("VEHICLE_ALREADY_RESERVED")]
    [InlineData("DRIVER_INACTIVE")]
    [InlineData("DRIVER_ALREADY_RESERVED")]
    [InlineData("DRIVER_UNQUALIFIED")]
    public void Rejected_eligibility_cannot_partially_assign_resources(string code)
    {
        var mission = Mission.Create(Guid.NewGuid(), MissionLocation.Create("A"), MissionLocation.Create("B"), RequiredCapacity.Create(1000));
        var now = DateTimeOffset.UtcNow;
        mission.Schedule(now.AddHours(1), now);
        var evidence = new AssignmentEligibility(code != "VEHICLE_INACTIVE", code == "VEHICLE_UNDER_MAINTENANCE", code == "INSUFFICIENT_VEHICLE_CAPACITY" ? 999 : 1000, code == "VEHICLE_ALREADY_RESERVED", code != "DRIVER_INACTIVE", code == "DRIVER_ALREADY_RESERVED", code != "DRIVER_UNQUALIFIED");
        var error = Assert.Throws<BusinessRuleValidationException>(() => mission.Assign(Guid.NewGuid(), Guid.NewGuid(), evidence));
        Assert.Equal(code, error.Rule.Code);
        Assert.Equal(MissionStatus.Scheduled, mission.Status);
        Assert.Null(mission.AssignedVehicleId);
        Assert.Null(mission.AssignedDriverId);
        Assert.False(mission.HasActiveReservation);
    }
}
