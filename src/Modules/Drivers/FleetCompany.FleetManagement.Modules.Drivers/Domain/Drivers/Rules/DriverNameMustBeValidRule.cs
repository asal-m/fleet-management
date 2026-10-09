using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;

public sealed class DriverNameMustBeValidRule(string? value) : BusinessRule("drivers", "DRIVER_NAME_INVALID", "drivers.name_invalid", null)
{
    public override bool IsBroken() => string.IsNullOrWhiteSpace(value) || value.Length > 100;
}
