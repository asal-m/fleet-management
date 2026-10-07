using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
namespace FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;

public sealed class MissionRepository<TContext>(TContext database) : IMissionRepository where TContext : DbContext
{
    public void Add(Mission mission) => database.Set<Mission>().Add(mission);
    public Task<Mission?> FindAsync(Guid id, CancellationToken ct) => database.Set<Mission>().SingleOrDefaultAsync(x => x.Id == id, ct);
}
