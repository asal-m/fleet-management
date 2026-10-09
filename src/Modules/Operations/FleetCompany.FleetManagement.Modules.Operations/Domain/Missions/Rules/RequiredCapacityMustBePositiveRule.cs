using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class RequiredCapacityMustBePositiveRule(int kilograms) : BusinessRule("operations", "REQUIRED_CAPACITY_INVALID", "operations.capacity_invalid", null)
{
    public override bool IsBroken() => kilograms <= 0;
}
