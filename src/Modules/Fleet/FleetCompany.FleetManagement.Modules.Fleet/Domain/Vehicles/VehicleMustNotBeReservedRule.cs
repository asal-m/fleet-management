using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

public sealed class VehicleMustNotBeReservedRule(bool hasActiveReservation)
    : BusinessRule("fleet", "VEHICLE_HAS_ACTIVE_MISSION", "fleet.vehicle_has_active_mission", null)
{
    public override bool IsBroken() => hasActiveReservation;
}
