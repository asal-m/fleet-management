using MPCore.Application.Results;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application;

public static class FleetFailures
{
    public static FailureDescriptor InvalidVehicleId() => new(
        new ErrorIdentity("fleet", "VEHICLE_ID_INVALID"),
        ErrorCategory.Validation,
        new FailureMessageDescriptor("fleet.vehicle_id_invalid", null),
        RetryDirective.Never, null);

    public static FailureDescriptor VehicleNotFound() => new(
        new ErrorIdentity("fleet", "VEHICLE_NOT_FOUND"),
        ErrorCategory.NotFound,
        new FailureMessageDescriptor("fleet.vehicle_not_found", null),
        RetryDirective.Never, null);

    public static FailureDescriptor DuplicatePlate() => new(
        new ErrorIdentity("fleet", "PLATE_NUMBER_ALREADY_REGISTERED"),
        ErrorCategory.Conflict,
        new FailureMessageDescriptor("fleet.plate_number_already_registered", null),
        RetryDirective.Never, null);
}
