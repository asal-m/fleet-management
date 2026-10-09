using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed record StartMaintenance(Guid Id) : ICommand<Result<VehicleDetailsView>>;
public sealed class StartMaintenanceHandler(VehicleWorkflow workflow, IResourceReservations reservations)
{
    public Task<Result<VehicleDetailsView>> Handle(StartMaintenance command, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        _ = unitOfWork;
        return workflow.Execute(command.Id, "MaintenanceStarted", async (vehicle, token) =>
        {
            vehicle.StartMaintenance(await reservations.VehicleReservedAsync(vehicle.Id, token));
            return true;
        }, ct);
    }
}
