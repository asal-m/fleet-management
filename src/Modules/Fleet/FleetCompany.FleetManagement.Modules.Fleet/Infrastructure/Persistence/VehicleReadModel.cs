using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;

public sealed class VehicleReadModel<TContext>(TContext database) : IVehicleReadModel
    where TContext : DbContext
{
    public async Task<VehicleDetailsView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await database.Set<Vehicle>().AsNoTracking()
            .Where(vehicle => vehicle.Id == id)
            .Select(vehicle => new
            {
                vehicle.Id,
                vehicle.PlateNumber,
                vehicle.TypeCode,
                vehicle.Capacity,
                vehicle.BaseStatus,
                vehicle.IsUnderMaintenance
            }).SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        var operationalStatus = VehicleStatus.Resolve(row.BaseStatus, row.IsUnderMaintenance);
        return new VehicleDetailsView(row.Id, row.PlateNumber.Value, row.TypeCode.Value,
            row.Capacity.Kilograms, row.BaseStatus, operationalStatus, row.IsUnderMaintenance);
    }
}
