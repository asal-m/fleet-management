using MPCore.Domain.Rules;

namespace FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Rules;

public sealed class DriverStatusMustBeValidRule(DriverStatus status) : BusinessRule("drivers", "DRIVER_STATUS_INVALID", "drivers.status_invalid", null)
{
    public override bool IsBroken() => status is not (DriverStatus.Active or DriverStatus.Inactive);
}
