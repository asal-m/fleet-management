using MPCore.Domain.Model;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;

namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
// Drivers owns its qualification code; it does not reference Fleet's internal domain.
public sealed class QualificationCode : ValueObject
{
    public string Value { get; }

    private QualificationCode(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        BusinessRules.Check(new QualificationCodeMustBeValidRule(normalized));
        Value = normalized!;
    }

    public static QualificationCode Create(string? value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
