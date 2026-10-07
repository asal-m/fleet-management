using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
namespace FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;

public sealed class MissionReadModel<TContext>(TContext database) : IMissionReadModel where TContext : DbContext
{
    public async Task<IReadOnlyList<MissionDetailsView>> ActiveAsync(int limit, CancellationToken ct)
    {
        var rows = await database.Set<Mission>().AsNoTracking().Where(x => x.Status == MissionStatus.Assigned || x.Status == MissionStatus.InProgress).OrderBy(x => x.Id).Take(limit)
            .Select(x => new { x.Id, x.Origin, x.Destination, x.RequiredCapacity, x.Status, x.ScheduledTime, x.AssignedVehicleId, x.AssignedDriverId }).ToListAsync(ct);
        return rows.Select(x => new MissionDetailsView(x.Id, x.Origin.Value, x.Destination.Value, x.RequiredCapacity.Kilograms, x.Status, x.ScheduledTime, x.AssignedVehicleId, x.AssignedDriverId)).ToArray();
    }
    public async Task<MissionDetailsView?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await database.Set<Mission>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Origin,
                x.Destination,
                x.RequiredCapacity,
                x.Status,
                x.ScheduledTime,
                x.AssignedVehicleId,
                x.AssignedDriverId
            })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new MissionDetailsView(row.Id, row.Origin.Value, row.Destination.Value,
            row.RequiredCapacity.Kilograms, row.Status, row.ScheduledTime, row.AssignedVehicleId, row.AssignedDriverId);
    }
}
