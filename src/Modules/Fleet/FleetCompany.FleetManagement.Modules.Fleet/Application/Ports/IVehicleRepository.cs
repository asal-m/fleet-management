using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;

public interface IVehicleRepository
{
    Task<Vehicle?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> PlateExistsAsync(PlateNumber plateNumber, CancellationToken cancellationToken);
    void Add(Vehicle vehicle);
}
