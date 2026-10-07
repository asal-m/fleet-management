using FluentValidation;
using FleetCompany.FleetManagement.Modules.Operations.Application.Queries;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Validators;

public sealed class GetActiveMissionsValidator : AbstractValidator<GetActiveMissions>
{
    public GetActiveMissionsValidator() { RuleFor(x => x.Limit).InclusiveBetween(1, 200).WithErrorCode("LIMIT_INVALID").WithMessage("operations.limit_invalid"); }
}
