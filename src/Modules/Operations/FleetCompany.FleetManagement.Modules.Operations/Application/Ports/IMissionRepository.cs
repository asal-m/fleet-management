using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Ports;

public interface IMissionRepository
{
    void Add(Mission mission);
    Task<Mission?> FindAsync(Guid id, CancellationToken cancellationToken);
}
