using MPCore.Application.Messaging;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Views;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;

namespace FleetCompany.FleetManagement.Modules.Drivers.Application.Queries;

public sealed record GetAvailableDrivers(int Limit = 50) : IQuery<Result<IReadOnlyList<RegisteredDriverView>>>;
public sealed class GetAvailableDriversHandler(IDriverLookup drivers, IResourceReservations reservations)
{
    public async Task<Result<IReadOnlyList<RegisteredDriverView>>> Handle(GetAvailableDrivers query, CancellationToken ct)
    {
        var rows = await drivers.AvailableAsync(await reservations.ReservedDriversAsync(ct), query.Limit, ct);
        return Result<IReadOnlyList<RegisteredDriverView>>.Success(rows.Select(d => new RegisteredDriverView(d.Id, d.FirstName, d.LastName, DriverStatus.Active, d.Qualifications)).ToArray());
    }
}
