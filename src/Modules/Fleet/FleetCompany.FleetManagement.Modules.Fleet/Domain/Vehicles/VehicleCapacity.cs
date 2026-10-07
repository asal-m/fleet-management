using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;
using MPCore.Domain.Model;
using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

public sealed class VehicleCapacity : ValueObject
{
    public int Kilograms { get; }

    private VehicleCapacity(int kilograms)
    {
        BusinessRules.Check(new VehicleCapacityMustBePositiveRule(kilograms));
        Kilograms = kilograms;
    }

    public static VehicleCapacity Create(int kilograms) => new(kilograms);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Kilograms;
    }
}
