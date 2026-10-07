using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record CancelMission(Guid Id) : ICommand<Result<MissionDetailsView>>;
public sealed class CancelMissionHandler(MissionWorkflow workflow)
{
    public Task<Result<MissionDetailsView>> Handle(CancelMission command, IUnitOfWork unitOfWork, CancellationToken ct) { _ = unitOfWork; return workflow.Cancel(command.Id, ct); }
}
