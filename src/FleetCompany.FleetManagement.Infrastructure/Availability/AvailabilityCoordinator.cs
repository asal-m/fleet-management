using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
namespace FleetCompany.FleetManagement.Infrastructure.Availability;

public sealed class AvailabilityCoordinator(AppDbContext database) : IAvailabilityCoordinator
{
    private static readonly System.Diagnostics.ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    public async Task AcquireAsync(CancellationToken cancellationToken)
    {
        using var span = Trace.StartActivity("Infrastructure.AvailabilityLock");
        if (database.Database.CurrentTransaction is null) throw new InvalidOperationException("Availability writes require the host transaction.");
        await database.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7382946150123)", cancellationToken);
    }
    public async Task<long> VersionAsync(CancellationToken cancellationToken)
     => await database.Database.SqlQueryRaw<long>("SELECT version AS \"Value\" FROM fleet_availability_revision WHERE id = 1").SingleAsync(cancellationToken);
    public Task ChangedAsync(CancellationToken cancellationToken)
     => database.Database.ExecuteSqlRawAsync("UPDATE fleet_availability_revision SET version = version + 1 WHERE id = 1", cancellationToken);
}
