using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Queries;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using Xunit;

namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class GetVehicleTests
{
    [Fact]
    public async Task Existing_vehicle_returns_view_and_forwards_cancellation()
    {
        var view = new VehicleDetailsView(Guid.NewGuid(), "AB-123", "TRUCK", 1000,
            VehicleBaseStatus.Active, VehicleOperationalStatus.Active, false);
        var port = new FakeReadModel(view);
        using var cancellation = new CancellationTokenSource();
        var result = await new GetVehicleHandler(port).Handle(new GetVehicle(view.Id), cancellation.Token);
        Assert.True(result.IsSuccess);
        Assert.Same(view, result.Value);
        Assert.Equal(view.Id, port.RequestedId);
        Assert.Equal(cancellation.Token, port.Token);
    }

    [Fact]
    public async Task Missing_vehicle_returns_stable_not_found_failure()
    {
        var result = await new GetVehicleHandler(new FakeReadModel(null)).Handle(new GetVehicle(Guid.NewGuid()), default);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.NotFound, result.FailureDescriptor!.Category);
        Assert.Equal("VEHICLE_NOT_FOUND", result.FailureDescriptor.Identity.Code);
    }

    private sealed class FakeReadModel(VehicleDetailsView? view) : IVehicleReadModel
    {
        public Guid RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<VehicleDetailsView?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            RequestedId = id;
            Token = cancellationToken;
            return Task.FromResult(view);
        }
    }
}
