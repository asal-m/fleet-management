using MPCore.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using FleetCompany.FleetManagement.Contracts;
namespace FleetCompany.FleetManagement.Infrastructure.Availability;

public sealed class AvailabilityCache(IReadThroughCache cache, ILogger<AvailabilityCache> logger) : IAvailabilityCache
{
    private static readonly System.Diagnostics.ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        using var span = Trace.StartActivity("Infrastructure.HybridCache");
        span?.SetTag("cache.key", key);
        try { return await cache.GetOrCreateAsync<T>(key, ct => new ValueTask<T>(factory(ct)), TimeSpan.FromSeconds(30), cancellationToken); }
        catch (Exception error) when (error is RedisException or TimeoutException)
        {
            logger.LogWarning("Availability cache unavailable ({FailureType}); reading database", error.GetType().Name);
            return await factory(cancellationToken);
        }
    }
}
