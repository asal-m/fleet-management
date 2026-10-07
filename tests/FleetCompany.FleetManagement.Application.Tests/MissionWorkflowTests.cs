using Microsoft.Extensions.Logging.Abstractions;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using FleetCompany.FleetManagement.Modules.Fleet.Contracts;
using FleetCompany.FleetManagement.Modules.Drivers.Contracts;
using FleetCompany.FleetManagement.Modules.Operations.Contracts;
using Xunit;
namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class MissionWorkflowTests
{
    [Theory]
    [InlineData("VEHICLE_INACTIVE")]
    [InlineData("VEHICLE_UNDER_MAINTENANCE")]
    [InlineData("INSUFFICIENT_VEHICLE_CAPACITY")]
    [InlineData("VEHICLE_ALREADY_RESERVED")]
    [InlineData("DRIVER_INACTIVE")]
    [InlineData("DRIVER_ALREADY_RESERVED")]
    [InlineData("DRIVER_UNQUALIFIED")]
    public async Task Assignment_failure_returns_stable_identity_and_audits_without_changing_mission(string code)
    {
        var m = Scheduled(); var audit = new TestAuditor(); var lookup = new Lookup(code); var repository = new Repository(m);
        var result = await Workflow(repository, lookup, audit).Assign(m.Id, lookup.Vehicle.Id, lookup.Driver.Id, default);
        Assert.True(result.IsFailure); Assert.Equal(ErrorCategory.Conflict, result.FailureDescriptor!.Category);
        Assert.Equal(code, result.FailureDescriptor.Identity.Code); Assert.Equal("operations", result.FailureDescriptor.Identity.Domain);
        Assert.Equal(MissionStatus.Scheduled, m.Status); Assert.Null(m.AssignedVehicleId); Assert.Null(m.AssignedDriverId);
        Assert.Empty(audit.Succeeded); Assert.Equal(code, Assert.Single(audit.Rejected));
    }
    [Fact]
    public async Task Success_and_exact_repeat_keep_one_assignment_and_one_business_audit()
    {
        var m = Scheduled(); var audit = new TestAuditor(); var lookup = new Lookup(""); var workflow = Workflow(new Repository(m), lookup, audit);
        var first = await workflow.Assign(m.Id, lookup.Vehicle.Id, lookup.Driver.Id, default); Assert.True(first.IsSuccess);
        var repeated = await workflow.Assign(m.Id, lookup.Vehicle.Id, lookup.Driver.Id, default); Assert.True(repeated.IsSuccess);
        Assert.Equal(lookup.Vehicle.Id, m.AssignedVehicleId); Assert.Equal(lookup.Driver.Id, m.AssignedDriverId);
        Assert.Equal("MissionAssigned", Assert.Single(audit.Succeeded)); Assert.Empty(audit.Rejected);
    }
    [Fact]
    public async Task Unknown_mission_returns_not_found_before_any_audit_or_change()
    {
        var audit = new TestAuditor(); var lookup = new Lookup("");
        var result = await Workflow(new Repository(null), lookup, audit).Assign(Guid.NewGuid(), lookup.Vehicle.Id, lookup.Driver.Id, default);
        Assert.True(result.IsFailure); Assert.Equal("MISSION_NOT_FOUND", result.FailureDescriptor!.Identity.Code);
        Assert.Empty(audit.Succeeded); Assert.Empty(audit.Rejected);
    }
    private static Mission Scheduled() { var m = Mission.Create(Guid.NewGuid(), MissionLocation.Create("A"), MissionLocation.Create("B"), RequiredCapacity.Create(1000)); var now = DateTimeOffset.UtcNow; m.Schedule(now.AddHours(1), now); return m; }
    private static MissionWorkflow Workflow(Repository repository, Lookup lookup, TestAuditor audit) => new(repository, new TestCoordinator(), audit, lookup, lookup, lookup, TimeProvider.System, NullLogger<MissionWorkflow>.Instance);
    private sealed class Repository(Mission? mission) : IMissionRepository
    {
        public void Add(Mission value) => throw new NotSupportedException();
        public Task<Mission?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(mission);
    }
    private sealed class Lookup(string code) : IFleetLookup, IDriverLookup, IResourceReservations
    {
        public VehicleSnapshot Vehicle { get; } = new(Guid.NewGuid(), "ABC", "TRUCK", code == "INSUFFICIENT_VEHICLE_CAPACITY" ? 999 : 1000, code != "VEHICLE_INACTIVE", code == "VEHICLE_UNDER_MAINTENANCE");
        public DriverSnapshot Driver { get; } = new(Guid.NewGuid(), "A", "B", code != "DRIVER_INACTIVE", code == "DRIVER_UNQUALIFIED" ? ["BUS"] : ["TRUCK"]);
        Task<VehicleSnapshot?> IFleetLookup.GetAsync(Guid id, CancellationToken ct) => Task.FromResult<VehicleSnapshot?>(Vehicle);
        Task<DriverSnapshot?> IDriverLookup.GetAsync(Guid id, CancellationToken ct) => Task.FromResult<DriverSnapshot?>(Driver);
        Task<IReadOnlyList<VehicleSnapshot>> IFleetLookup.AvailableAsync(IReadOnlyCollection<Guid> ids, int limit, CancellationToken ct) => throw new NotSupportedException();
        Task<IReadOnlyList<DriverSnapshot>> IDriverLookup.AvailableAsync(IReadOnlyCollection<Guid> ids, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> VehicleReservedAsync(Guid id, CancellationToken ct) => Task.FromResult(code == "VEHICLE_ALREADY_RESERVED");
        public Task<bool> DriverReservedAsync(Guid id, CancellationToken ct) => Task.FromResult(code == "DRIVER_ALREADY_RESERVED");
        public Task<IReadOnlyCollection<Guid>> ReservedVehiclesAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Guid>> ReservedDriversAsync(CancellationToken ct) => throw new NotSupportedException();
    }
}
