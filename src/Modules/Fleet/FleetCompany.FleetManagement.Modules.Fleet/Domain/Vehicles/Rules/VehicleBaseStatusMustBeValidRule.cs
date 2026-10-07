using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

public sealed class VehicleBaseStatusMustBeValidRule(VehicleBaseStatus status)
    : BusinessRule("fleet", "VEHICLE_BASE_STATUS_INVALID", "fleet.vehicle_base_status_invalid", null)
{
    public override bool IsBroken() => status is not (VehicleBaseStatus.Active or VehicleBaseStatus.Inactive);
}
