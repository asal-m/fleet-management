using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

public sealed class ResourceMustExistRule(bool exists, string code, string key) : BusinessRule("operations", code, "operations." + key, null)
{
    public override bool IsBroken() => !exists;
}
