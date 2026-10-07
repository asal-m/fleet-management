using MPCore.Application.Results;
using MPCore.Audit;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed class MissionWorkflow(IMissionRepository repository, IAvailabilityCoordinator coordinator, IBusinessAuditRecorder audit,
 IFleetLookup fleet, IDriverLookup drivers, IResourceReservations reservations, TimeProvider clock, Microsoft.Extensions.Logging.ILogger<MissionWorkflow> logger)
{
    private static readonly System.Diagnostics.ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    public async Task<Result<MissionDetailsView>> Execute(Guid id, string action, Func<Mission, Task<bool>> operation, CancellationToken ct)
    {
        using var applicationSpan = Trace.StartActivity("Application." + action);
        Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(logger, "Mission operation {Action}; entity {EntityId}; trace {TraceId}", action, id, System.Diagnostics.Activity.Current?.TraceId.ToString());
        await coordinator.AcquireAsync(ct);
        var mission = await repository.FindAsync(id, ct);
        if (mission is null) return Result<MissionDetailsView>.FromFailure(OperationsFailures.MissionNotFound());
        try
        {
            var changed = await operation(mission);
            if (changed)
            {
                await audit.RecordAsync("operations", action, nameof(Mission), mission.Id.ToString(), null, ct);
                await coordinator.ChangedAsync(ct);
            }
            return Result<MissionDetailsView>.Success(MissionView.From(mission));
        }
        catch (BusinessRuleValidationException error)
        {
            applicationSpan?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, error.Rule.Code);
            var rule = error.Rule;
            await audit.RecordAttemptAsync("operations", action, AuditOutcome.Rejected, new AuditFailure(rule.ErrorDomain, rule.Code), rule.Code, nameof(Mission), id.ToString(), null, ct);
            return Result<MissionDetailsView>.FromFailure(new(new ErrorIdentity(rule.ErrorDomain, rule.Code), ErrorCategory.Conflict, new FailureMessageDescriptor(rule.MessageKey, rule.MessageArguments), RetryDirective.Never, null));
        }
    }
    public Task<Result<MissionDetailsView>> Schedule(Guid id, DateTimeOffset time, CancellationToken ct)
    => Execute(id, "MissionScheduled", m => Task.FromResult(m.Schedule(time, clock.GetUtcNow())), ct);
    public Task<Result<MissionDetailsView>> Start(Guid id, CancellationToken ct) => Execute(id, "MissionStarted", m => Task.FromResult(m.Start()), ct);
    public Task<Result<MissionDetailsView>> Complete(Guid id, CancellationToken ct) => Execute(id, "MissionCompleted", m => Task.FromResult(m.Complete()), ct);
    public Task<Result<MissionDetailsView>> Cancel(Guid id, CancellationToken ct) => Execute(id, "MissionCancelled", m => Task.FromResult(m.Cancel()), ct);
    public Task<Result<MissionDetailsView>> Assign(Guid id, Guid vehicleId, Guid driverId, CancellationToken ct)
    => Execute(id, "MissionAssigned", async m =>
    {
        if (m.Status == MissionStatus.Assigned && m.AssignedVehicleId == vehicleId && m.AssignedDriverId == driverId) return false;
        var vehicle = await fleet.GetAsync(vehicleId, ct);
        var driver = await drivers.GetAsync(driverId, ct);
        BusinessRules.Check(new ResourceMustExistRule(vehicle is not null, "VEHICLE_NOT_FOUND", "vehicle_not_found"));
        BusinessRules.Check(new ResourceMustExistRule(driver is not null, "DRIVER_NOT_FOUND", "driver_not_found"));
        var evidence = new AssignmentEligibility(vehicle!.IsActive, vehicle.IsUnderMaintenance, vehicle.CapacityKilograms,
     await reservations.VehicleReservedAsync(vehicleId, ct), driver!.IsActive, await reservations.DriverReservedAsync(driverId, ct),
     driver.Qualifications.Contains(vehicle.TypeCode, StringComparer.Ordinal));
        return m.Assign(vehicleId, driverId, evidence);
    }, ct);
}
