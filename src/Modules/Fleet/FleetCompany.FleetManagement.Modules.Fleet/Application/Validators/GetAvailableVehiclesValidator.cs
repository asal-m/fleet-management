using FluentValidation;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Queries;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Validators;

public sealed class GetAvailableVehiclesValidator : AbstractValidator<GetAvailableVehicles>
{
    public GetAvailableVehiclesValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 200).WithErrorCode("LIMIT_INVALID").WithMessage("fleet.limit_invalid");
    }
}
