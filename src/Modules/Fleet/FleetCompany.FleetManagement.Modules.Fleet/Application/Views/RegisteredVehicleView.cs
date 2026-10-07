using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Views;

public sealed record RegisteredVehicleView(Guid Id, string PlateNumber, string TypeCode,
    int CapacityKilograms, VehicleBaseStatus BaseStatus, VehicleOperationalStatus OperationalStatus,
    bool IsUnderMaintenance);
