using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

namespace FleetCompany.FleetManagement.Modules.Drivers.Infrastructure.Persistence;

public sealed class DriverLookup<TContext>(TContext database) : IDriverLookup where TContext : DbContext
{
    private static DriverSnapshot View(Driver d) => new(d.Id, d.FirstName.Value, d.LastName.Value, d.IsActive, d.Qualifications.Select(x => x.Value).ToArray());
    public async Task<DriverSnapshot?> GetAsync(Guid id, CancellationToken ct)
    {
        var d = await database.Set<Driver>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return d is null ? null : View(d);
    }

    public async Task<IReadOnlyList<DriverSnapshot>> AvailableAsync(IReadOnlyCollection<Guid> excluded, int limit, CancellationToken ct) => (await database.Set<Driver>().AsNoTracking().Where(x => x.Status == DriverStatus.Active && !excluded.Contains(x.Id)).OrderBy(x => x.Id).Take(limit).ToListAsync(ct)).Select(View).ToArray();
}
