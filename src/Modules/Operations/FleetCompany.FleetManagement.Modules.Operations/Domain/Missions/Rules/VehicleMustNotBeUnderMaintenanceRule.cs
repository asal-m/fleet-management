using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class VehicleMustNotBeUnderMaintenanceRule(bool maintenance) : BusinessRule("operations", "VEHICLE_UNDER_MAINTENANCE", "operations.vehicle_under_maintenance", null)
{
    public override bool IsBroken() => maintenance;
}
