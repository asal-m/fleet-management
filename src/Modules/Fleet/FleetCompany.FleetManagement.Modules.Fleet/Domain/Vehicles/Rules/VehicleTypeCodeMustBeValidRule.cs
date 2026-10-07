using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

public sealed class VehicleTypeCodeMustBeValidRule(string? value)
    : BusinessRule("fleet", "VEHICLE_TYPE_CODE_INVALID", "fleet.vehicle_type_code_invalid", null)
{
    public override bool IsBroken() => value is null || value.Length is < 1 or > 50
        || value.Any(character => character is not (>= 'A' and <= 'Z')
            and not (>= '0' and <= '9') and not '_');
}
