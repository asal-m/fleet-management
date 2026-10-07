using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using MPCore.Audit;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed class VehicleWorkflow(IVehicleRepository repository, IAvailabilityCoordinator coordinator, IResourceReservations reservations, IBusinessAuditRecorder audit)
{
    public async Task<Result<Guid>> Execute(Guid id, string action, VehicleBaseStatus? status, CancellationToken ct)
    {
        await coordinator.AcquireAsync(ct); var v = await repository.FindAsync(id, ct);
        if (v is null) return Result<Guid>.FromFailure(FleetFailures.VehicleNotFound());
        try
        {
            var hasReservation = (action == "MaintenanceStarted" || status == VehicleBaseStatus.Inactive)
                && await reservations.VehicleReservedAsync(id, ct);
            if (action == "MaintenanceStarted") v.StartMaintenance(hasReservation);
            else if (action == "MaintenanceCompleted") v.CompleteMaintenance();
            else { if (v.BaseStatus == status) return Result<Guid>.Success(id); v.ChangeBaseStatus(status!.Value, hasReservation); }
            await audit.RecordAsync("fleet", action, nameof(Vehicle), id.ToString(), null, ct);
            await coordinator.ChangedAsync(ct);
            return Result<Guid>.Success(id);
        }
        catch (BusinessRuleValidationException error)
        {
            var rule = error.Rule;
            await audit.RecordAttemptAsync("fleet", action, AuditOutcome.Rejected, new(rule.ErrorDomain, rule.Code), rule.Code, nameof(Vehicle), id.ToString(), null, ct);
            return Result<Guid>.FromFailure(new(new(rule.ErrorDomain, rule.Code), ErrorCategory.Conflict, new(rule.MessageKey, rule.MessageArguments), RetryDirective.Never, null));
        }
    }
}
