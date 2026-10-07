using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
namespace FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;

public sealed class FleetLookup<TContext>(TContext database) : IFleetLookup where TContext : DbContext
{
    public async Task<VehicleSnapshot?> GetAsync(Guid id, CancellationToken ct)
    {
        var v = await database.Set<Vehicle>().AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.PlateNumber, x.TypeCode, x.Capacity, x.BaseStatus, x.IsUnderMaintenance }).SingleOrDefaultAsync(ct);
        return v is null ? null : new(v.Id, v.PlateNumber.Value, v.TypeCode.Value, v.Capacity.Kilograms, v.BaseStatus == VehicleBaseStatus.Active, v.IsUnderMaintenance);
    }
    public async Task<IReadOnlyList<VehicleSnapshot>> AvailableAsync(IReadOnlyCollection<Guid> excluded, int limit, CancellationToken ct)
    {
        var rows = await database.Set<Vehicle>().AsNoTracking().Where(x => x.BaseStatus == VehicleBaseStatus.Active && !x.IsUnderMaintenance && !excluded.Contains(x.Id)).OrderBy(x => x.Id).Take(limit).Select(x => new { x.Id, x.PlateNumber, x.TypeCode, x.Capacity }).ToListAsync(ct);
        return rows.Select(x => new VehicleSnapshot(x.Id, x.PlateNumber.Value, x.TypeCode.Value, x.Capacity.Kilograms, true, false)).ToArray();
    }
}
