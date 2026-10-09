using MPCore.Domain.Model;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

public sealed class VehicleTypeCode : ValueObject
{
    public string Value { get; }

    private VehicleTypeCode(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant();
        BusinessRules.Check(new VehicleTypeCodeMustBeValidRule(normalized));
        Value = normalized!;
    }

    public static VehicleTypeCode Create(string? value) => new(value);
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
