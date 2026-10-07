using MPCore.Domain.Rules;
namespace FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;

public sealed class DriverMustBeUnreservedRule(bool reserved) : BusinessRule("operations", "DRIVER_ALREADY_RESERVED", "operations.driver_already_reserved", null) { public override bool IsBroken() => reserved; }
