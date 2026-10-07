using FluentValidation;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Validators;

public sealed class CreateMissionValidator : AbstractValidator<CreateMission>
{
    public CreateMissionValidator()
    {
        RuleFor(x => x.Origin).Must(ValidLocation).WithErrorCode("MISSION_LOCATION_INVALID").WithMessage("operations.location_invalid");
        RuleFor(x => x.Destination).Must(ValidLocation).WithErrorCode("MISSION_LOCATION_INVALID").WithMessage("operations.location_invalid");
        RuleFor(x => x.RequiredCapacityKilograms).GreaterThan(0)
            .WithErrorCode("REQUIRED_CAPACITY_INVALID").WithMessage("operations.capacity_invalid");
    }
    private static bool ValidLocation(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 500;
}
