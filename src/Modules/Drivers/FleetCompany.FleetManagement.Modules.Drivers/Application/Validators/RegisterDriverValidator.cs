using FluentValidation;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Commands;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Validators;

public sealed class RegisterDriverValidator : AbstractValidator<RegisterDriver>
{
    public RegisterDriverValidator()
    {
        RuleFor(x => x.FirstName).Must(ValidName).WithErrorCode("DRIVER_NAME_INVALID").WithMessage("drivers.name_invalid");
        RuleFor(x => x.LastName).Must(ValidName).WithErrorCode("DRIVER_NAME_INVALID").WithMessage("drivers.name_invalid");
        RuleFor(x => x.Status).Must(x => x is DriverStatus.Active or DriverStatus.Inactive).WithErrorCode("DRIVER_STATUS_INVALID").WithMessage("drivers.status_invalid");
        RuleFor(x => x.Qualifications).Must(x => x is { Count: > 0 }).WithErrorCode("DRIVER_QUALIFICATIONS_REQUIRED").WithMessage("drivers.qualifications_required");
        RuleForEach(x => x.Qualifications).Must(ValidCode).WithErrorCode("QUALIFICATION_CODE_INVALID").WithMessage("drivers.qualification_code_invalid");
    }

    private static bool ValidName(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 100;
    private static bool ValidCode(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        return code is { Length: >= 1 and <= 50 } && code.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_');
    }
}
