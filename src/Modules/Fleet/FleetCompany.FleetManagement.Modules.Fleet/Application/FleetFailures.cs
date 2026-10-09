using MPCore.Application.Results;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application;

public static class FleetFailures
{
    public static FailureDescriptor InvalidBaseStatus() => new(new("fleet", "VEHICLE_BASE_STATUS_INVALID"), ErrorCategory.Validation, new("fleet.vehicle_base_status_invalid", null), RetryDirective.Never, [new ValidationFailureDetail([new FieldViolation("base_status", "VEHICLE_BASE_STATUS_INVALID", new("fleet.vehicle_base_status_invalid", null))])]);
    public static FailureDescriptor FromRule(IBusinessRule rule) => new(new(rule.ErrorDomain, rule.Code), rule is VehicleMustNotBeReservedRule ? ErrorCategory.Conflict : ErrorCategory.BusinessRule, new(rule.MessageKey, rule.MessageArguments), RetryDirective.Never, null);
    public static FailureDescriptor InvalidVehicleId() => new(new ErrorIdentity("fleet", "VEHICLE_ID_INVALID"), ErrorCategory.Validation, new FailureMessageDescriptor("fleet.vehicle_id_invalid", null), RetryDirective.Never, null);
    public static FailureDescriptor VehicleNotFound() => new(new ErrorIdentity("fleet", "VEHICLE_NOT_FOUND"), ErrorCategory.NotFound, new FailureMessageDescriptor("fleet.vehicle_not_found", null), RetryDirective.Never, null);
    public static FailureDescriptor DuplicatePlate() => new(new ErrorIdentity("fleet", "PLATE_NUMBER_ALREADY_REGISTERED"), ErrorCategory.Conflict, new FailureMessageDescriptor("fleet.plate_number_already_registered", null), RetryDirective.Never, null);
}
