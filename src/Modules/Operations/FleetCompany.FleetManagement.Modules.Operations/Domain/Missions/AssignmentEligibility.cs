namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

public sealed record AssignmentEligibility(bool VehicleActive, bool VehicleUnderMaintenance, int VehicleCapacity, bool VehicleReserved, bool DriverActive, bool DriverReserved, bool DriverQualified);
