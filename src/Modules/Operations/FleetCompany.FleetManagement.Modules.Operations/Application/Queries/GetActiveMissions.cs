using MPCore.Application.Messaging;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Queries;

public sealed record GetActiveMissions(int Limit = 50) : IQuery<Result<IReadOnlyList<MissionDetailsView>>>;
public sealed class GetActiveMissionsHandler(IMissionReadModel reader)
{
    public async Task<Result<IReadOnlyList<MissionDetailsView>>> Handle(GetActiveMissions query, CancellationToken ct)
    => Result<IReadOnlyList<MissionDetailsView>>.Success(await reader.ActiveAsync(query.Limit, ct));
}
