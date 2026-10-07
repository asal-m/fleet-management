using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Queries;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using Xunit;
namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class GetMissionTests
{
    [Fact]
    public async Task Existing_mission_returns_view_and_forwards_id_and_cancellation()
    {
        var view = new MissionDetailsView(Guid.NewGuid(), "تهران", "شیراز", 1000,
            MissionStatus.Draft, null, null, null);
        var readModel = new ReadModel(view);
        using var cancellation = new CancellationTokenSource();
        var result = await new GetMissionHandler(readModel).Handle(new GetMission(view.Id), cancellation.Token);
        Assert.True(result.IsSuccess);
        Assert.Same(view, result.Value);
        Assert.Equal(view.Id, readModel.Id);
        Assert.Equal(cancellation.Token, readModel.Token);
    }
    [Fact]
    public async Task Missing_mission_returns_stable_not_found_identity()
    {
        var result = await new GetMissionHandler(new ReadModel(null)).Handle(new GetMission(Guid.NewGuid()), default);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.NotFound, result.FailureDescriptor!.Category);
        Assert.Equal("operations", result.FailureDescriptor.Identity.Domain);
        Assert.Equal("MISSION_NOT_FOUND", result.FailureDescriptor.Identity.Code);
    }
    private sealed class ReadModel(MissionDetailsView? view) : IMissionReadModel
    {
        public Task<IReadOnlyList<MissionDetailsView>> ActiveAsync(int limit, CancellationToken ct) => throw new NotSupportedException();
        public Guid Id { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<MissionDetailsView?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            Id = id;
            Token = cancellationToken;
            return Task.FromResult(view);
        }
    }
}
