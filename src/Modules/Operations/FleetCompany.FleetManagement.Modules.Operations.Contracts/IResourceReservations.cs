namespace FleetCompany.FleetManagement.Modules.Operations.Contracts;

public interface IResourceReservations
{
    Task<bool> VehicleReservedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DriverReservedAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> ReservedVehiclesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> ReservedDriversAsync(CancellationToken cancellationToken);
}
