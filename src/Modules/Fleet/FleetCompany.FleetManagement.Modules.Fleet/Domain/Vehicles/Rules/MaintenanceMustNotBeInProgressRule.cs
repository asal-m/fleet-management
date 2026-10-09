using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;

public sealed class MaintenanceMustNotBeInProgressRule(bool isUnderMaintenance) : BusinessRule("fleet", "MAINTENANCE_ALREADY_STARTED", "fleet.maintenance_already_started", null)
{
    public override bool IsBroken() => isUnderMaintenance;
}
