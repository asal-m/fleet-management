using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

public sealed class VehicleCapacityMustBePositiveRule(int kilograms) : BusinessRule("fleet", "VEHICLE_CAPACITY_MUST_BE_POSITIVE", "fleet.vehicle_capacity_must_be_positive", null)
{
    public override bool IsBroken() => kilograms <= 0;
}
