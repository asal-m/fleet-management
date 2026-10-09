using MPCore.Application.Messaging;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Views;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Queries;

public sealed record GetDriver(Guid Id) : IQuery<Result<RegisteredDriverView>>;
public sealed class GetDriverHandler(IDriverLookup drivers)
{
    public async Task<Result<RegisteredDriverView>> Handle(GetDriver query, CancellationToken ct)
    {
        var driver = await drivers.GetAsync(query.Id, ct);
        if (driver is null)
            return Result<RegisteredDriverView>.FromFailure(new FailureDescriptor(new("drivers", "DRIVER_NOT_FOUND"), ErrorCategory.NotFound, new("drivers.driver_not_found", null), RetryDirective.Never, null));
        return Result<RegisteredDriverView>.Success(new(driver.Id, driver.FirstName, driver.LastName, driver.IsActive ? DriverStatus.Active : DriverStatus.Inactive, driver.Qualifications));
    }
}
