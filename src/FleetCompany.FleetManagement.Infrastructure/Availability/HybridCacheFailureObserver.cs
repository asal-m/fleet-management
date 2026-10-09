using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace FleetCompany.FleetManagement.Infrastructure.Availability;
// Observe the installed HybridCache's diagnostic event; never inspect its payload or cache keys.
// The library absorbs L2 failures, so a try/catch around IReadThroughCache cannot see every fallback.
public sealed class HybridCacheFailureObserver : EventListener
{
    private static readonly AsyncLocal<Observation?> Current = new();
    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == "Microsoft-Extensions-HybridCache")
            EnableEvents(source, EventLevel.Error);
    }

    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if (data.EventSource.Name == "Microsoft-Extensions-HybridCache" && data.EventName == "DistributedCacheFailed")
            Current.Value?.MarkFailure();
    }

    public Observation Begin() => new();
    public sealed class Observation : IDisposable
    {
        private readonly Observation? previous;
        private int failed;
        internal Observation()
        {
            previous = Current.Value;
            Current.Value = this;
        }

        public bool BackendFailed => Volatile.Read(ref failed) != 0;

        internal void MarkFailure()
        {
            if (Interlocked.Exchange(ref failed, 1) == 0)
                Activity.Current?.AddEvent(new ActivityEvent("cache.distributed_failure"));
        }

        public void Dispose() => Current.Value = previous;
    }
}
