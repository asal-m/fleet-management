using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class DriverMustBeActiveRule(bool active) : BusinessRule("operations", "DRIVER_INACTIVE", "operations.driver_inactive", null) { public override bool IsBroken() => !active; }
