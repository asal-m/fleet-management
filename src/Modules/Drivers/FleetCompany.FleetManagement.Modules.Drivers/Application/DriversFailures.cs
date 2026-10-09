using MPCore.Application.Results;

namespace FleetCompany.FleetManagement.Modules.Drivers.Application;

public static class DriversFailures
{
    public static FailureDescriptor InvalidStatus() => new(new("drivers", "DRIVER_STATUS_INVALID"), ErrorCategory.Validation, new("drivers.status_invalid", null), RetryDirective.Never, [new ValidationFailureDetail([new FieldViolation("status", "DRIVER_STATUS_INVALID", new("drivers.status_invalid", null))])]);
}
