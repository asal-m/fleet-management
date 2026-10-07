using MPCore.Application.Messaging;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Queries;

public sealed record GetAvailableVehicles(int Limit = 50) : IQuery<Result<IReadOnlyList<VehicleDetailsView>>>;
public sealed class GetAvailableVehiclesHandler(IFleetLookup fleet, IResourceReservations reservations, IAvailabilityCoordinator coordinator, IAvailabilityCache cache)
{
    public async Task<Result<IReadOnlyList<VehicleDetailsView>>> Handle(GetAvailableVehicles query, CancellationToken ct)
    {
        async Task<VehicleDetailsView[]> Read(CancellationToken token)
        {
            var excluded = await reservations.ReservedVehiclesAsync(token);
            var rows = await fleet.AvailableAsync(excluded, query.Limit, token);
            return rows.Select(v => new VehicleDetailsView(v.Id, v.PlateNumber, v.TypeCode, v.CapacityKilograms, VehicleBaseStatus.Active, VehicleOperationalStatus.Active, false)).ToArray();
        }
        var version = await coordinator.VersionAsync(ct);
        var rows = await cache.GetOrCreateAsync($"fleet:available:v1:{version}:limit:{query.Limit}", Read, ct);
        if (version != await coordinator.VersionAsync(ct)) rows = await Read(ct);
        return Result<IReadOnlyList<VehicleDetailsView>>.Success(rows);
    }
}
