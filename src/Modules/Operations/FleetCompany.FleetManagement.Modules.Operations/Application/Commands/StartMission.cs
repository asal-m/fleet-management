using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record StartMission(Guid Id) : ICommand<Result<MissionDetailsView>>;
public sealed class StartMissionHandler(MissionWorkflow workflow)
{
    public Task<Result<MissionDetailsView>> Handle(StartMission command, IUnitOfWork unitOfWork, CancellationToken ct) { _ = unitOfWork; return workflow.Start(command.Id, ct); }
}
