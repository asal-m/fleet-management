using FluentValidation;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Validators;

public sealed class ChangeVehicleStatusValidator : AbstractValidator<ChangeVehicleStatus>
{
    public ChangeVehicleStatusValidator()
    {
        RuleFor(x => x.Status).Must(x => x is VehicleBaseStatus.Active or VehicleBaseStatus.Inactive).WithErrorCode("VEHICLE_BASE_STATUS_INVALID").WithMessage("fleet.vehicle_base_status_invalid");
    }
}
