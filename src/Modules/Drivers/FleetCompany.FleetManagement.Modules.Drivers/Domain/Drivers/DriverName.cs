using MPCore.Domain.Model;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;
namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

public sealed class DriverName : ValueObject
{
    public string Value { get; }
    private DriverName(string? value)
    {
        var normalized = value?.Trim();
        BusinessRules.Check(new DriverNameMustBeValidRule(normalized));
        Value = normalized!;
    }
    public static DriverName Create(string? value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
