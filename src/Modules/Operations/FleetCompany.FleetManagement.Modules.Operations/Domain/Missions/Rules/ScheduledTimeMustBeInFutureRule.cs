using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class ScheduledTimeMustBeInFutureRule(DateTimeOffset scheduled, DateTimeOffset now) : BusinessRule("operations", "SCHEDULED_TIME_NOT_FUTURE", "operations.scheduled_time_not_future", null)
{
    public override bool IsBroken() => scheduled <= now;
}
