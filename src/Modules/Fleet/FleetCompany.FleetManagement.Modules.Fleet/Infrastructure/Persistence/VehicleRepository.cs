using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;

public sealed class VehicleRepository<TContext>(TContext database) : IVehicleRepository
    where TContext : DbContext
{
    public Task<Vehicle?> FindAsync(Guid id, CancellationToken ct) => database.Set<Vehicle>().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> PlateExistsAsync(PlateNumber plateNumber, CancellationToken cancellationToken)
        => database.Set<Vehicle>().AnyAsync(vehicle => vehicle.PlateNumber == plateNumber, cancellationToken);

    public void Add(Vehicle vehicle) => database.Set<Vehicle>().Add(vehicle);
}
