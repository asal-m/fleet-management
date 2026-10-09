using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Views;

public sealed record VehicleDetailsView(Guid Id, string PlateNumber, string TypeCode, int CapacityKilograms, VehicleBaseStatus BaseStatus, VehicleOperationalStatus OperationalStatus, bool IsUnderMaintenance)
{
    public string BaseStatusName => BaseStatus.ToString();
    public string OperationalStatusName => OperationalStatus.ToString();
}
