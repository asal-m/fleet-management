using MPCore.Domain.Model;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

public sealed class Vehicle : AggregateRoot<Guid>
{
    private Vehicle()
    {
        PlateNumber = null!;
        TypeCode = null!;
        Capacity = null!;
    }

    public PlateNumber PlateNumber { get; private set; }
    public VehicleTypeCode TypeCode { get; private set; }
    public VehicleCapacity Capacity { get; private set; }
    public VehicleBaseStatus BaseStatus { get; private set; }
    public bool IsUnderMaintenance { get; private set; }
    public VehicleOperationalStatus OperationalStatus => VehicleStatus.Resolve(BaseStatus, IsUnderMaintenance);
    // This checks only Fleet's own state, not Mission reservations in Operations.
    public bool IsOperationalForAssignment => BaseStatus == VehicleBaseStatus.Active && !IsUnderMaintenance;

    private Vehicle(Guid id, PlateNumber plateNumber, VehicleTypeCode typeCode, VehicleCapacity capacity, VehicleBaseStatus baseStatus) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(plateNumber);
        ArgumentNullException.ThrowIfNull(typeCode);
        ArgumentNullException.ThrowIfNull(capacity);
        CheckRule(new VehicleBaseStatusMustBeValidRule(baseStatus));
        PlateNumber = plateNumber;
        TypeCode = typeCode;
        Capacity = capacity;
        BaseStatus = baseStatus;
    }

    public static Vehicle Register(Guid id, PlateNumber plateNumber, VehicleTypeCode typeCode, VehicleCapacity capacity, VehicleBaseStatus baseStatus) => new(id, plateNumber, typeCode, capacity, baseStatus);
    // Application must also coordinate the Operations reservation check in D10/D11.
    public void ChangeBaseStatus(VehicleBaseStatus status, bool hasActiveReservation)
    {
        CheckRule(new VehicleBaseStatusMustBeValidRule(status));
        if (status == VehicleBaseStatus.Inactive)
            CheckRule(new VehicleMustNotBeReservedRule(hasActiveReservation));
        BaseStatus = status;
    }

    // Application must also coordinate the Operations reservation check in D09/D11.
    public void StartMaintenance(bool hasActiveReservation)
    {
        CheckRule(new VehicleMustNotBeReservedRule(hasActiveReservation));
        CheckRule(new MaintenanceMustNotBeInProgressRule(IsUnderMaintenance));
        IsUnderMaintenance = true;
    }

    public void CompleteMaintenance()
    {
        CheckRule(new MaintenanceMustBeInProgressRule(IsUnderMaintenance));
        IsUnderMaintenance = false;
    }
}
