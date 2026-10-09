using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class DriverMustBeQualifiedRule(bool qualified) : BusinessRule("operations", "DRIVER_UNQUALIFIED", "operations.driver_unqualified", null)
{
    public override bool IsBroken() => !qualified;
}
