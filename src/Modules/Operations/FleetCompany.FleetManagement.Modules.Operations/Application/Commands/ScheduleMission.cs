using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record ScheduleMission(Guid Id, DateTimeOffset ScheduledTime) : ICommand<Result<MissionDetailsView>>;
public sealed class ScheduleMissionHandler(MissionWorkflow workflow)
{
    public Task<Result<MissionDetailsView>> Handle(ScheduleMission command, IUnitOfWork unitOfWork, CancellationToken ct) { _ = unitOfWork; return workflow.Schedule(command.Id, command.ScheduledTime, ct); }
}
