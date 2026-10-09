using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record ScheduleMission(Guid Id, string? ScheduledTime) : ICommand<Result<MissionDetailsView>>;
public sealed class ScheduleMissionHandler(MissionWorkflow workflow, TimeProvider clock)
{
    public Task<Result<MissionDetailsView>> Handle(ScheduleMission command, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        _ = unitOfWork;
        ct.ThrowIfCancellationRequested();
        if (command.Id == Guid.Empty)
            return Task.FromResult(Result<MissionDetailsView>.FromFailure(OperationsFailures.InvalidMissionId()));
        if (!ScheduledTimestamp.TryParse(command.ScheduledTime, out var time))
            return Task.FromResult(Result<MissionDetailsView>.FromFailure(OperationsFailures.InvalidScheduledTime()));
        return workflow.Execute(command.Id, "MissionScheduled", mission => MissionWorkflow.DomainCall("Schedule", () => mission.Schedule(time, clock.GetUtcNow())), ct);
    }
}

public static class ScheduledTimestamp
{
    public static bool TryParse(string? value, out DateTimeOffset time)
    {
        time = default;
        return value is not null
            && Regex.IsMatch(value, @"(Z|[+-]\d{2}:\d{2})$")
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}
