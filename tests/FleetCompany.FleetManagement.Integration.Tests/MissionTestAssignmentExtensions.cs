using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Integration.Tests;
// Persistence fixtures only, not the real assignment use case.
public static class MissionTestAssignmentExtensions
{
    public static bool Assign(this Mission mission, Guid vehicle, Guid driver) => mission.Assign(vehicle, driver, new(true, false, int.MaxValue, false, true, false, true));
}
