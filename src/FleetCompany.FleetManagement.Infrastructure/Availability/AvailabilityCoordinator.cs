using Microsoft.EntityFrameworkCore;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace FleetCompany.FleetManagement.Infrastructure.Availability;

public sealed class AvailabilityCoordinator(AppDbContext database) : IAvailabilityCoordinator
{
    private static readonly ActivitySource Trace = new("FleetCompany.FleetManagement.Business");

    public Task AcquireMissionAsync(Guid missionId, CancellationToken cancellationToken)
        => AcquireAsync("mission", missionId, cancellationToken);

    public async Task AcquireResourcesAsync(Guid? vehicleId, Guid? driverId, CancellationToken cancellationToken)
    {
        // Every caller takes mission (when applicable), driver, then vehicle. Never reverse this order.
        if (driverId.HasValue)
            await AcquireAsync("driver", driverId.Value, cancellationToken);
        if (vehicleId.HasValue)
            await AcquireAsync("vehicle", vehicleId.Value, cancellationToken);
    }

    public static long LockKey(string scope, Guid id)
        => BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes($"fleet:{scope}:{id:D}")));

    private async Task AcquireAsync(string scope, Guid id, CancellationToken cancellationToken)
    {
        using var span = Trace.StartActivity("Infrastructure.AvailabilityLock");
        if (database.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Availability writes require the host transaction.");
        var key = LockKey(scope, id);
        await database.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    public async Task<long> VersionAsync(CancellationToken cancellationToken) => await database.Database.SqlQueryRaw<long>("SELECT version AS \"Value\" FROM fleet_availability_revision WHERE id = 1").SingleAsync(cancellationToken);
    public Task ChangedAsync(CancellationToken cancellationToken) => database.Database.ExecuteSqlRawAsync("UPDATE fleet_availability_revision SET version = version + 1 WHERE id = 1", cancellationToken);
}
