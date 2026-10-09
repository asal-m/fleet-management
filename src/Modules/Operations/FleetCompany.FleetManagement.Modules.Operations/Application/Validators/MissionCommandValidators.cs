using FluentValidation;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Validators;

public sealed class ScheduleMissionValidator : AbstractValidator<ScheduleMission>
{
    public ScheduleMissionValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithErrorCode("MISSION_ID_INVALID").WithMessage("operations.mission_id_invalid");
        RuleFor(x => x.ScheduledTime).Must(value => ScheduledTimestamp.TryParse(value, out _)).WithErrorCode("SCHEDULED_TIME_INVALID").WithMessage("operations.scheduled_time_invalid");
    }
}

public sealed class AssignMissionValidator : AbstractValidator<AssignMission>
{
    public AssignMissionValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithErrorCode("MISSION_ID_INVALID").WithMessage("operations.mission_id_invalid");
        RuleFor(x => x.VehicleId).NotEmpty().WithErrorCode("MISSION_RESOURCE_ID_INVALID").WithMessage("operations.resource_id_invalid");
        RuleFor(x => x.DriverId).NotEmpty().WithErrorCode("MISSION_RESOURCE_ID_INVALID").WithMessage("operations.resource_id_invalid");
    }
}
