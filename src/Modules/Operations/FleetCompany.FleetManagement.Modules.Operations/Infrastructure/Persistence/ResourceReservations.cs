using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
namespace FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;

public sealed class ResourceReservations<TContext>(TContext database) : IResourceReservations where TContext : DbContext
{
    private IQueryable<Mission> Active() => database.Set<Mission>().AsNoTracking().Where(x => x.Status == MissionStatus.Assigned || x.Status == MissionStatus.InProgress);
    public Task<bool> VehicleReservedAsync(Guid id, CancellationToken ct) => Active().AnyAsync(x => x.AssignedVehicleId == id, ct);
    public Task<bool> DriverReservedAsync(Guid id, CancellationToken ct) => Active().AnyAsync(x => x.AssignedDriverId == id, ct);
    public async Task<IReadOnlyCollection<Guid>> ReservedVehiclesAsync(CancellationToken ct) => await Active().Select(x => x.AssignedVehicleId!.Value).Distinct().ToArrayAsync(ct);
    public async Task<IReadOnlyCollection<Guid>> ReservedDriversAsync(CancellationToken ct) => await Active().Select(x => x.AssignedDriverId!.Value).Distinct().ToArrayAsync(ct);
}
