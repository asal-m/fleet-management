using FleetCompany.FleetManagement.Modules.Operations.Application.Views;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Ports;

public interface IMissionReadModel
{
    Task<MissionDetailsView?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MissionDetailsView>> ActiveAsync(int limit, CancellationToken cancellationToken);
}
