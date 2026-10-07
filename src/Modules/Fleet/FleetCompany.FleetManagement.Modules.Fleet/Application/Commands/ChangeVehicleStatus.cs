using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed record ChangeVehicleStatus(Guid Id, VehicleBaseStatus Status) : ICommand<Result<Guid>>;
public sealed class ChangeVehicleStatusHandler(VehicleWorkflow workflow)
{
    public Task<Result<Guid>> Handle(ChangeVehicleStatus command, IUnitOfWork unitOfWork, CancellationToken ct) { _ = unitOfWork; return workflow.Execute(command.Id, "VehicleStatusChanged", command.Status, ct); }
}
