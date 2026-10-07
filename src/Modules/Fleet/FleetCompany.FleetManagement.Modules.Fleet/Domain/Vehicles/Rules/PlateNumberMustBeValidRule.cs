using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

public sealed class PlateNumberMustBeValidRule(string? value)
    : BusinessRule("fleet", "PLATE_NUMBER_INVALID", "fleet.plate_number_invalid", null)
{
    public override bool IsBroken() => string.IsNullOrWhiteSpace(value)
        || value.Length > 30;
}
