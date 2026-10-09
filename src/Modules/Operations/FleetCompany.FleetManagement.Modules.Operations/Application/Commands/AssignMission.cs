using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record AssignMission(Guid Id, Guid VehicleId, Guid DriverId) : ICommand<Result<MissionDetailsView>>;
public sealed class AssignMissionHandler(MissionWorkflow workflow, IFleetLookup fleet, IDriverLookup drivers, IResourceReservations reservations)
{
    public Task<Result<MissionDetailsView>> Handle(AssignMission command, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        _ = unitOfWork;
        return workflow.Execute(command.Id, "MissionAssigned", async mission =>
        {
            if (mission.Status == MissionStatus.Assigned && mission.AssignedVehicleId == command.VehicleId && mission.AssignedDriverId == command.DriverId)
                return false;
            if (mission.Status == MissionStatus.Assigned)
                return await MissionWorkflow.DomainCall(nameof(Mission.Assign), () => mission.Assign(command.VehicleId, command.DriverId, null!));
            await workflow.LockAssignmentResources(command.VehicleId, command.DriverId, ct);
            var vehicle = await fleet.GetAsync(command.VehicleId, ct);
            var driver = await drivers.GetAsync(command.DriverId, ct);
            BusinessRules.Check(new ResourceMustExistRule(vehicle is not null, "VEHICLE_NOT_FOUND", "vehicle_not_found"));
            BusinessRules.Check(new ResourceMustExistRule(driver is not null, "DRIVER_NOT_FOUND", "driver_not_found"));
            var evidence = new AssignmentEligibility(vehicle!.IsActive, vehicle.IsUnderMaintenance, vehicle.CapacityKilograms, await reservations.VehicleReservedAsync(command.VehicleId, ct), driver!.IsActive, await reservations.DriverReservedAsync(command.DriverId, ct), driver.Qualifications.Contains(vehicle.TypeCode, StringComparer.Ordinal));
            return await MissionWorkflow.DomainCall(nameof(Mission.Assign), () => mission.Assign(command.VehicleId, command.DriverId, evidence));
        }, ct);
    }
}
