using FluentValidation;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Queries;

namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Validators;

public sealed class GetAvailableDriversValidator : AbstractValidator<GetAvailableDrivers>
{
    public GetAvailableDriversValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 200).WithErrorCode("LIMIT_INVALID").WithMessage("drivers.limit_invalid");
    }
}
