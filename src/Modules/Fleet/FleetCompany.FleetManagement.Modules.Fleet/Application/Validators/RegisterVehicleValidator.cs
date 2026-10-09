using FluentValidation;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Validators;

public sealed class RegisterVehicleValidator : AbstractValidator<RegisterVehicle>
{
    public RegisterVehicleValidator()
    {
        RuleFor(command => command.PlateNumber).Must(value => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 30).WithErrorCode("PLATE_NUMBER_INVALID").WithMessage("fleet.plate_number_invalid");
        RuleFor(command => command.TypeCode).Must(value => value is not null && IsValidType(value.Trim().ToUpperInvariant())).WithErrorCode("VEHICLE_TYPE_CODE_INVALID").WithMessage("fleet.vehicle_type_code_invalid");
        RuleFor(command => command.CapacityKilograms).GreaterThan(0).WithErrorCode("VEHICLE_CAPACITY_MUST_BE_POSITIVE").WithMessage("fleet.vehicle_capacity_must_be_positive");
        RuleFor(command => command.BaseStatus).Must(value => value is VehicleBaseStatus.Active or VehicleBaseStatus.Inactive).WithErrorCode("VEHICLE_BASE_STATUS_INVALID").WithMessage("fleet.vehicle_base_status_invalid");
    }

    private static bool IsValidType(string value) => value.Length is >= 1 and <= 50 && value.All(character => character is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_');
}
