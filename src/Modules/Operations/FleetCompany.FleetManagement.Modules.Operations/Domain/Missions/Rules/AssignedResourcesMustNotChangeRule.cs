using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class AssignedResourcesMustNotChangeRule(Guid? existingVehicle, Guid? existingDriver, Guid vehicle, Guid driver)
    : BusinessRule("operations", "MISSION_REASSIGNMENT_FORBIDDEN", "operations.reassignment_forbidden", null)
{
    public override bool IsBroken() => existingVehicle != vehicle || existingDriver != driver;
}
