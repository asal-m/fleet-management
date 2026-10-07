using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class VehicleMustBeOperationalRule(bool active) : BusinessRule("operations", "VEHICLE_INACTIVE", "operations.vehicle_inactive", null) { public override bool IsBroken() => !active; }
