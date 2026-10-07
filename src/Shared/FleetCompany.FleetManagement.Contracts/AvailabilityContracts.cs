namespace FleetCompany.FleetManagement.Contracts;

public interface IAvailabilityCoordinator
{
    Task AcquireAsync(CancellationToken cancellationToken);
    Task<long> VersionAsync(CancellationToken cancellationToken);
    Task ChangedAsync(CancellationToken cancellationToken);
}
public interface IAvailabilityCache
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken);
}
