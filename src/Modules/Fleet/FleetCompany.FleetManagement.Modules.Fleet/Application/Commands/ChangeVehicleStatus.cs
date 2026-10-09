using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed record ChangeVehicleStatus(Guid Id, VehicleBaseStatus Status) : ICommand<Result<VehicleDetailsView>>;
public sealed class ChangeVehicleStatusHandler(VehicleWorkflow workflow, IResourceReservations reservations)
{
    public Task<Result<VehicleDetailsView>> Handle(ChangeVehicleStatus command, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        _ = unitOfWork;
        return workflow.Execute(command.Id, "VehicleStatusChanged", async (vehicle, token) =>
        {
            if (vehicle.BaseStatus == command.Status)
                return false;
            var reserved = command.Status == VehicleBaseStatus.Inactive && await reservations.VehicleReservedAsync(vehicle.Id, token);
            vehicle.ChangeBaseStatus(command.Status, reserved);
            return true;
        }, ct);
    }
}
