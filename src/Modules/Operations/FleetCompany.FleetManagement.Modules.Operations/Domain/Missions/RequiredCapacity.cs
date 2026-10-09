using MPCore.Domain.Model;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

public sealed class RequiredCapacity : ValueObject
{
    public int Kilograms { get; }

    private RequiredCapacity(int kilograms)
    {
        BusinessRules.Check(new RequiredCapacityMustBePositiveRule(kilograms));
        Kilograms = kilograms;
    }

    public static RequiredCapacity Create(int kilograms) => new(kilograms);
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Kilograms;
    }
}
