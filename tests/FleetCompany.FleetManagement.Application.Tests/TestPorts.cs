using FleetCompany.FleetManagement.Contracts;
using MPCore.Audit;
namespace FleetCompany.FleetManagement.Application.Tests;
// Unit-test substitutes only; production uses transactional coordination and real MP Core audit.
public sealed class TestCoordinator : IAvailabilityCoordinator
{
    public Task AcquireAsync(CancellationToken ct) => Task.CompletedTask;
    public Task<long> VersionAsync(CancellationToken ct) => Task.FromResult(0L);
    public Task ChangedAsync(CancellationToken ct) => Task.CompletedTask;
}
public sealed class TestAuditor : IBusinessAuditRecorder
{
    public List<string> Succeeded { get; } = [];
    public List<string> Rejected { get; } = [];
    public ValueTask RecordAsync(string module, string action, string? entityType, string? entityId, IReadOnlyDictionary<string, string>? metadata, CancellationToken ct) { Succeeded.Add(action); return ValueTask.CompletedTask; }
    public ValueTask RecordAttemptAsync(string module, string action, AuditOutcome outcome, AuditFailure? failure, string? reason, string? entityType, string? entityId, IReadOnlyDictionary<string, string>? metadata, CancellationToken ct) { Rejected.Add(failure?.Code ?? ""); return ValueTask.CompletedTask; }
}
