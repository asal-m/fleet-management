using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
namespace FleetCompany.FleetManagement.Domain.Tests;
// Domain fixtures only. Runtime eligibility always comes from verified module Contracts under a lock.
public static class MissionTestAssignmentExtensions
{
    public static bool Assign(this Mission mission, Guid vehicle, Guid driver) => mission.Assign(vehicle, driver, new(true, false, int.MaxValue, false, true, false, true));
}
