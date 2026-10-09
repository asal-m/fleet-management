using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

public sealed class MaintenanceMustBeInProgressRule(bool isUnderMaintenance) : BusinessRule("fleet", "MAINTENANCE_NOT_STARTED", "fleet.maintenance_not_started", null)
{
    public override bool IsBroken() => !isUnderMaintenance;
}
