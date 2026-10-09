using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using MPCore.Application.Results;
using MPCore.Audit;
using MPCore.Domain.Rules;
using System.Diagnostics;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed class VehicleWorkflow(IVehicleRepository repository, IAvailabilityCoordinator coordinator, IBusinessAuditRecorder audit)
{
    private static readonly ActivitySource Trace = new("FleetCompany.FleetManagement.Business");
    public async Task<Result<VehicleDetailsView>> Execute(Guid id, string action, Func<Vehicle, CancellationToken, Task<bool>> operation, CancellationToken ct)
    {
        using var span = Trace.StartActivity("Application." + action);
        await coordinator.AcquireResourcesAsync(id, null, ct);
        var v = await repository.FindAsync(id, ct);
        if (v is null)
            return Result<VehicleDetailsView>.FromFailure(FleetFailures.VehicleNotFound());
        try
        {
            var operationalBefore = v.IsOperationalForAssignment;
            if (!await operation(v, ct))
                return Result<VehicleDetailsView>.Success(View(v));
            await audit.RecordAsync("fleet", action, nameof(Vehicle), id.ToString(), null, ct);
            if (operationalBefore != v.IsOperationalForAssignment)
                await coordinator.ChangedAsync(ct);
            return Result<VehicleDetailsView>.Success(View(v));
        }
        catch (BusinessRuleValidationException error)
        {
            span?.SetStatus(ActivityStatusCode.Error, error.Rule.Code);
            var rule = error.Rule;
            await audit.RecordAttemptAsync("fleet", action, AuditOutcome.Rejected, new(rule.ErrorDomain, rule.Code), rule.Code, nameof(Vehicle), id.ToString(), null, ct);
            return Result<VehicleDetailsView>.FromFailure(FleetFailures.FromRule(rule));
        }
    }

    private static VehicleDetailsView View(Vehicle v) => new(v.Id, v.PlateNumber.Value, v.TypeCode.Value, v.Capacity.Kilograms, v.BaseStatus, v.OperationalStatus, v.IsUnderMaintenance);
}
