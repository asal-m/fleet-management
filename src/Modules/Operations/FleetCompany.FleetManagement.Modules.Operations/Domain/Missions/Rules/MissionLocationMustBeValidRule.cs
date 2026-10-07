using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class MissionLocationMustBeValidRule(string? value)
    : BusinessRule("operations", "MISSION_LOCATION_INVALID", "operations.location_invalid", null)
{
    public override bool IsBroken() => string.IsNullOrWhiteSpace(value) || value.Length > 500;
}
