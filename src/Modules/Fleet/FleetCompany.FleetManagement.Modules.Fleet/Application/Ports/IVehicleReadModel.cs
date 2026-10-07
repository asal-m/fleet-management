using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;

public interface IVehicleReadModel
{
    Task<VehicleDetailsView?> GetAsync(Guid id, CancellationToken cancellationToken);
}
