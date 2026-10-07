using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;

public sealed class DriverQualificationsMustBeValidRule(IReadOnlyCollection<QualificationCode> qualifications)
    : BusinessRule("drivers", "DRIVER_QUALIFICATIONS_REQUIRED", "drivers.qualifications_required", null)
{
    public override bool IsBroken() => qualifications.Count == 0 || qualifications.Any(code => code is null);
}
