using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed record CompleteMaintenance(Guid Id) : ICommand<Result<Guid>>;
public sealed class CompleteMaintenanceHandler(VehicleWorkflow workflow)
{
    public Task<Result<Guid>> Handle(CompleteMaintenance command, IUnitOfWork unitOfWork, CancellationToken ct) { _ = unitOfWork; return workflow.Execute(command.Id, "MaintenanceCompleted", null, ct); }
}
