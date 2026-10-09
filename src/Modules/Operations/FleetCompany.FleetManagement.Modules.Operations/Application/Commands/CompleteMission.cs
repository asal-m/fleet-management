using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record CompleteMission(Guid Id) : ICommand<Result<MissionDetailsView>>;
public sealed class CompleteMissionHandler(MissionWorkflow workflow)
{
    public Task<Result<MissionDetailsView>> Handle(CompleteMission command, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        _ = unitOfWork;
        return workflow.Execute(command.Id, "MissionCompleted", mission => MissionWorkflow.DomainCall("Complete", mission.Complete), ct);
    }
}
