using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class MissionResourceIdsMustBeValidRule(Guid vehicleId, Guid driverId)
    : BusinessRule("operations", "MISSION_RESOURCE_ID_INVALID", "operations.resource_id_invalid", null)
{
    public override bool IsBroken() => vehicleId == Guid.Empty || driverId == Guid.Empty;
}
