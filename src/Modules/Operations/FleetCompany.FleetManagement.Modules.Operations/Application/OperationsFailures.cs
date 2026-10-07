using MPCore.Application.Results;
namespace FleetCompany.FleetManagement.Modules.Operations.Application;

public static class OperationsFailures
{
    public static FailureDescriptor InvalidScheduledTime() => new(new("operations", "SCHEDULED_TIME_INVALID"), ErrorCategory.Validation, new("operations.scheduled_time_invalid", null), RetryDirective.Never, null);
    public static FailureDescriptor InvalidMissionId() => new(new("operations", "MISSION_ID_INVALID"), ErrorCategory.Validation, new("operations.mission_id_invalid", null), RetryDirective.Never, null);
    public static FailureDescriptor MissionNotFound() => new(
        new ErrorIdentity("operations", "MISSION_NOT_FOUND"), ErrorCategory.NotFound,
        new FailureMessageDescriptor("operations.mission_not_found", null), RetryDirective.Never, null);
}
