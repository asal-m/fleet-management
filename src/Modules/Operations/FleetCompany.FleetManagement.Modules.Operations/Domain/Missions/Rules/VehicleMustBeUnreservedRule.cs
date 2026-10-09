using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class VehicleMustBeUnreservedRule(bool reserved) : BusinessRule("operations", "VEHICLE_ALREADY_RESERVED", "operations.vehicle_already_reserved", null)
{
    public override bool IsBroken() => reserved;
}
