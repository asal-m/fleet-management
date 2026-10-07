namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

public static class VehicleStatus
{
    public static VehicleOperationalStatus Resolve(VehicleBaseStatus baseStatus, bool isUnderMaintenance)
        => isUnderMaintenance
            ? VehicleOperationalStatus.UnderMaintenance
            : baseStatus == VehicleBaseStatus.Active
                ? VehicleOperationalStatus.Active : VehicleOperationalStatus.Inactive;
}
