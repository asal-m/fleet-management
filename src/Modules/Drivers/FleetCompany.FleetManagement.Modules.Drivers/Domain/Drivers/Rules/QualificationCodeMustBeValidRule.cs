using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;

public sealed class QualificationCodeMustBeValidRule(string? value)
    : BusinessRule("drivers", "QUALIFICATION_CODE_INVALID", "drivers.qualification_code_invalid", null)
{
    public override bool IsBroken() => string.IsNullOrEmpty(value) || value.Length > 50
        || value.Any(character => !(character is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_'));
}
