namespace FleetCompany.FleetManagement.Modules.Fleet.Contracts;

public sealed record VehicleSnapshot(Guid Id, string PlateNumber, string TypeCode, int CapacityKilograms, bool IsActive, bool IsUnderMaintenance);
public interface IFleetLookup
{
    Task<VehicleSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleSnapshot>> AvailableAsync(IReadOnlyCollection<Guid> excluded, int limit, CancellationToken cancellationToken);
}
