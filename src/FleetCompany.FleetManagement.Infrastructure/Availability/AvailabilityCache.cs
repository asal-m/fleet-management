using MPCore.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FleetCompany.FleetManagement.Contracts;

namespace FleetCompany.FleetManagement.Infrastructure.Availability;

public sealed class AvailabilityCache(IReadThroughCache cache, ILogger<AvailabilityCache> logger, HybridCacheFailureObserver observer) : IAvailabilityCache
{
    private static readonly ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    private static readonly Meter Meter = new("FleetCompany.FleetManagement.Availability");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("fleet.availability.cache.requests");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("fleet.availability.cache.duration", "ms");
    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        using var span = Trace.StartActivity("Infrastructure.HybridCache");
        using var observation = observer.Begin();
        var started = Stopwatch.GetTimestamp();
        var created = 0;
        var outcome = "hit";
        try
        {
            var value = await cache.GetOrCreateAsync<T>(key, async ct =>
            {
                Interlocked.Exchange(ref created, 1);
                return await factory(ct);
            }, TimeSpan.FromSeconds(30), cancellationToken);
            outcome = observation.BackendFailed ? "fallback" : created == 0 ? "hit" : "miss";
            return value;
        }
        catch (Exception error) when (error is RedisException or TimeoutException)
        {
            outcome = "fallback";
            span?.SetTag("cache.failure_type", error.GetType().Name);
            logger.LogWarning("Availability cache unavailable ({FailureType}); reading database", error.GetType().Name);
            return await factory(cancellationToken);
        }
        finally
        {
            span?.SetTag("cache.outcome", outcome);
            var tags = new TagList
            {
                {
                    "cache.outcome",
                    outcome
                }
            };
            Requests.Add(1, tags);
            Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, tags);
        }
    }
}
