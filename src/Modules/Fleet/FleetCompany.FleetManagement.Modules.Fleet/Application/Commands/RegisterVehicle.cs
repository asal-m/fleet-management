using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;

public sealed record RegisterVehicle(string? PlateNumber, string? TypeCode, int CapacityKilograms,
    VehicleBaseStatus? BaseStatus) : ICommand<Result<RegisteredVehicleView>>
{
    public override string ToString() => nameof(RegisterVehicle);
}

public sealed class RegisterVehicleHandler(IVehicleRepository repository, IAvailabilityCoordinator coordinator)
{
    public async Task<Result<RegisteredVehicleView>> Handle(RegisterVehicle command,
        IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        // Declaring the port selects the host's transaction middleware; do not commit here.
        _ = unitOfWork;
        await coordinator.AcquireAsync(cancellationToken);
        var plate = PlateNumber.Create(command.PlateNumber);
        var type = VehicleTypeCode.Create(command.TypeCode);
        var capacity = VehicleCapacity.Create(command.CapacityKilograms);
        var status = command.BaseStatus ?? throw new ArgumentNullException(nameof(command.BaseStatus));

        if (await repository.PlateExistsAsync(plate, cancellationToken))
            return Result<RegisteredVehicleView>.FromFailure(FleetFailures.DuplicatePlate());

        var vehicle = Vehicle.Register(Guid.CreateVersion7(), plate, type, capacity, status);
        repository.Add(vehicle);
        await coordinator.ChangedAsync(cancellationToken);
        return Result<RegisteredVehicleView>.Success(new RegisteredVehicleView(vehicle.Id,
            plate.Value, type.Value, capacity.Kilograms, vehicle.BaseStatus,
            vehicle.OperationalStatus, vehicle.IsUnderMaintenance));
    }
}
