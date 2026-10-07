namespace FleetCompany.FleetManagement.Modules.Drivers.Contracts;

public sealed record DriverSnapshot(Guid Id, string FirstName, string LastName, bool IsActive, string[] Qualifications);
public interface IDriverLookup
{
    Task<DriverSnapshot?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DriverSnapshot>> AvailableAsync(IReadOnlyCollection<Guid> excluded, int limit, CancellationToken cancellationToken);
}
