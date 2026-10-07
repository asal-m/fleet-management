using FluentValidation;

namespace FleetCompany.FleetManagement.Modules.Administration.Application.Queries;

public sealed class GetAuditValidator : AbstractValidator<GetAudit>
{
    public GetAuditValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).WithErrorCode("PAGE_INVALID").WithMessage("administration.page_invalid");
        RuleFor(x => x.Size).InclusiveBetween(1, 200).WithErrorCode("SIZE_INVALID").WithMessage("administration.size_invalid");
        RuleFor(x => x.EntityId).MaximumLength(100).WithErrorCode("ENTITY_ID_INVALID").WithMessage("administration.entity_id_invalid");
    }
}
