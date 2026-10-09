using MPCore.Application.Messaging;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Queries;

public sealed record GetMission(Guid Id) : IQuery<Result<MissionDetailsView>>;
public sealed class GetMissionHandler(IMissionReadModel readModel)
{
    public async Task<Result<MissionDetailsView>> Handle(GetMission query, CancellationToken cancellationToken)
    {
        var mission = await readModel.GetAsync(query.Id, cancellationToken);
        return mission is null ? Result<MissionDetailsView>.FromFailure(OperationsFailures.MissionNotFound()) : Result<MissionDetailsView>.Success(mission);
    }
}
