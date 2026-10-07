using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class MissionStateMustAllowOperationRule(MissionStatus current, params MissionStatus[] allowed)
    : BusinessRule("operations", "MISSION_STATE_INVALID", "operations.state_invalid", null)
{
    public override bool IsBroken() => !allowed.Contains(current);
}
