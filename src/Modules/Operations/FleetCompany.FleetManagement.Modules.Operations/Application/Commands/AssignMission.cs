using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record AssignMission(Guid Id, Guid VehicleId, Guid DriverId) : ICommand<Result<MissionDetailsView>>;
public sealed class AssignMissionHandler(MissionWorkflow workflow)
{
    public Task<Result<MissionDetailsView>> Handle(AssignMission command, IUnitOfWork unitOfWork, CancellationToken ct) { _ = unitOfWork; return workflow.Assign(command.Id, command.VehicleId, command.DriverId, ct); }
}
