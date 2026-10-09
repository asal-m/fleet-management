using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class VehicleCapacityMustSatisfyMissionRule(int available, int required) : BusinessRule("operations", "INSUFFICIENT_VEHICLE_CAPACITY", "operations.insufficient_vehicle_capacity", null)
{
    public override bool IsBroken() => available < required;
}
