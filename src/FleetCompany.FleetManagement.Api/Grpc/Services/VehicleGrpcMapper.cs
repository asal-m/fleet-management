using FleetCompany.FleetManagement.Api.Grpc.Fleet;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Api.Grpc.Services;

public static class VehicleGrpcMapper
{
    public static VehicleDetails Map(VehicleDetailsView view) => new()
    {
        Id = view.Id.ToString(),
        PlateNumber = view.PlateNumber,
        TypeCode = view.TypeCode,
        CapacityKilograms = view.CapacityKilograms,
        BaseStatus = view.BaseStatus switch
        {
            VehicleBaseStatus.Active => VehicleBaseState.Active,
            VehicleBaseStatus.Inactive => VehicleBaseState.Inactive,
            _ => throw new InvalidOperationException("Unexpected stored vehicle base status.")
        },
        OperationalStatus = view.OperationalStatus switch
        {
            VehicleOperationalStatus.Active => VehicleOperationalState.Active,
            VehicleOperationalStatus.Inactive => VehicleOperationalState.Inactive,
            VehicleOperationalStatus.UnderMaintenance => VehicleOperationalState.UnderMaintenance,
            _ => throw new InvalidOperationException("Unexpected vehicle operational status.")
        },
        IsUnderMaintenance = view.IsUnderMaintenance
    };
}
