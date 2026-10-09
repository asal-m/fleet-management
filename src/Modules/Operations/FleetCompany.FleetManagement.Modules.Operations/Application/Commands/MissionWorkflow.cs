using MPCore.Application.Results;
using MPCore.Audit;
using MPCore.Domain.Rules;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed class MissionWorkflow(IMissionRepository repository, IAvailabilityCoordinator coordinator, IBusinessAuditRecorder audit, ILogger<MissionWorkflow> logger)
{
    private static readonly ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    public async Task<Result<MissionDetailsView>> Execute(Guid id, string action, Func<Mission, Task<bool>> operation, CancellationToken ct)
    {
        using var applicationSpan = Trace.StartActivity("Application." + action);
        logger.LogInformation("Mission operation {Action}; entity {EntityId}", action, id);
        await coordinator.AcquireMissionAsync(id, ct);
        var mission = await repository.FindAsync(id, ct);
        if (mission is null)
            return Result<MissionDetailsView>.FromFailure(OperationsFailures.MissionNotFound());
        try
        {
            if (mission.HasActiveReservation)
                await coordinator.AcquireResourcesAsync(mission.AssignedVehicleId, mission.AssignedDriverId, ct);
            var reservedBefore = mission.HasActiveReservation;
            var changed = await operation(mission);
            if (changed)
            {
                await audit.RecordAsync("operations", action, nameof(Mission), mission.Id.ToString(), null, ct);
                if (reservedBefore != mission.HasActiveReservation)
                    await coordinator.ChangedAsync(ct);
            }

            return Result<MissionDetailsView>.Success(MissionView.From(mission));
        }
        catch (BusinessRuleValidationException error)
        {
            applicationSpan?.SetStatus(ActivityStatusCode.Error, error.Rule.Code);
            var rule = error.Rule;
            await audit.RecordAttemptAsync("operations", action, AuditOutcome.Rejected, new AuditFailure(rule.ErrorDomain, rule.Code), rule.Code, nameof(Mission), id.ToString(), null, ct);
            return Result<MissionDetailsView>.FromFailure(OperationsFailures.FromRule(rule));
        }
    }

    public static Task<bool> DomainCall(string operation, Func<bool> invoke)
    {
        using var span = Trace.StartActivity("Domain.Mission." + operation);
        return Task.FromResult(invoke());
    }

    public Task LockAssignmentResources(Guid vehicleId, Guid driverId, CancellationToken ct)
        => coordinator.AcquireResourcesAsync(vehicleId, driverId, ct);
}
