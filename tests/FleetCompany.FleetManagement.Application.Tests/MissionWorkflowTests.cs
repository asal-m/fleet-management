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
    [InlineData("VEHICLE_INACTIVE", ErrorCategory.BusinessRule)]
    [InlineData("VEHICLE_UNDER_MAINTENANCE", ErrorCategory.BusinessRule)]
    [InlineData("INSUFFICIENT_VEHICLE_CAPACITY", ErrorCategory.BusinessRule)]
    [InlineData("VEHICLE_ALREADY_RESERVED", ErrorCategory.Conflict)]
    [InlineData("DRIVER_INACTIVE", ErrorCategory.BusinessRule)]
    [InlineData("DRIVER_ALREADY_RESERVED", ErrorCategory.Conflict)]
    [InlineData("DRIVER_UNQUALIFIED", ErrorCategory.BusinessRule)]
    [InlineData("VEHICLE_NOT_FOUND", ErrorCategory.NotFound)]
    [InlineData("DRIVER_NOT_FOUND", ErrorCategory.NotFound)]
    public async Task Assignment_failure_returns_stable_identity_and_audits_without_changing_mission(string code, ErrorCategory category)
    {
        var m = Scheduled();
        var audit = new TestAuditor();
        var lookup = new Lookup(code);
        var repository = new Repository(m);
        var result = await Assign(Workflow(repository, lookup, audit), lookup, m.Id);
        Assert.True(result.IsFailure);
        Assert.Equal(category, result.FailureDescriptor!.Category);
        Assert.Equal(code, result.FailureDescriptor.Identity.Code);
        Assert.Equal("operations", result.FailureDescriptor.Identity.Domain);
        Assert.Equal(MissionStatus.Scheduled, m.Status);
        Assert.Null(m.AssignedVehicleId);
        Assert.Null(m.AssignedDriverId);
        Assert.Empty(audit.Succeeded);
        Assert.Equal(code, Assert.Single(audit.Rejected));
    }

    [Fact]
    public async Task Success_and_exact_repeat_keep_one_assignment_and_one_business_audit()
    {
        var m = Scheduled();
        var audit = new TestAuditor();
        var lookup = new Lookup("");
        var workflow = Workflow(new Repository(m), lookup, audit);
        var first = await Assign(workflow, lookup, m.Id);
        Assert.True(first.IsSuccess);
        var repeated = await Assign(workflow, lookup, m.Id);
        Assert.True(repeated.IsSuccess);
        Assert.Equal(lookup.Vehicle.Id, m.AssignedVehicleId);
        Assert.Equal(lookup.Driver.Id, m.AssignedDriverId);
        Assert.Equal("MissionAssigned", Assert.Single(audit.Succeeded));
        Assert.Empty(audit.Rejected);
    }

    [Fact]
    public async Task Unknown_mission_returns_not_found_before_any_audit_or_change()
    {
        var audit = new TestAuditor();
        var lookup = new Lookup("");
        var result = await Assign(Workflow(new Repository(null), lookup, audit), lookup, Guid.NewGuid());
        Assert.True(result.IsFailure);
        Assert.Equal("MISSION_NOT_FOUND", result.FailureDescriptor!.Identity.Code);
        Assert.Empty(audit.Succeeded);
        Assert.Empty(audit.Rejected);
    }

    private static Task<Result<FleetCompany.FleetManagement.Modules.Operations.Application.Views.MissionDetailsView>> Assign(MissionWorkflow workflow, Lookup lookup, Guid id) => new AssignMissionHandler(workflow, lookup, lookup, lookup).Handle(new(id, lookup.Vehicle.Id, lookup.Driver.Id), new NoCommit(), default);
    private sealed class NoCommit : MPCore.Persistence.Abstractions.IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Only middleware may commit.");
    }

    private static Mission Scheduled()
    {
        var m = Mission.Create(Guid.NewGuid(), MissionLocation.Create("A"), MissionLocation.Create("B"), RequiredCapacity.Create(1000));
        var now = DateTimeOffset.UtcNow;
        m.Schedule(now.AddHours(1), now);
        return m;
    }

    private static MissionWorkflow Workflow(Repository repository, Lookup lookup, TestAuditor audit) => new(repository, new TestCoordinator(), audit, NullLogger<MissionWorkflow>.Instance);
    private sealed class Repository(Mission? mission) : IMissionRepository
    {
        public void Add(Mission value) => throw new NotSupportedException();
        public Task<Mission?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(mission);
    }

    private sealed class Lookup(string code) : IFleetLookup, IDriverLookup, IResourceReservations
    {
        public VehicleSnapshot Vehicle { get; } = new(Guid.NewGuid(), "ABC", "TRUCK", code == "INSUFFICIENT_VEHICLE_CAPACITY" ? 999 : 1000, code != "VEHICLE_INACTIVE", code == "VEHICLE_UNDER_MAINTENANCE");
        public DriverSnapshot Driver { get; } = new(Guid.NewGuid(), "A", "B", code != "DRIVER_INACTIVE", code == "DRIVER_UNQUALIFIED" ? ["BUS"] : ["TRUCK"]);

        Task<VehicleSnapshot?> IFleetLookup.GetAsync(Guid id, CancellationToken ct) => Task.FromResult<VehicleSnapshot?>(code == "VEHICLE_NOT_FOUND" ? null : Vehicle);
        Task<DriverSnapshot?> IDriverLookup.GetAsync(Guid id, CancellationToken ct) => Task.FromResult<DriverSnapshot?>(code == "DRIVER_NOT_FOUND" ? null : Driver);
        Task<IReadOnlyList<VehicleSnapshot>> IFleetLookup.AvailableAsync(IReadOnlyCollection<Guid> ids, int limit, CancellationToken ct) => throw new NotSupportedException();
        Task<IReadOnlyList<DriverSnapshot>> IDriverLookup.AvailableAsync(IReadOnlyCollection<Guid> ids, int limit, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> VehicleReservedAsync(Guid id, CancellationToken ct) => Task.FromResult(code == "VEHICLE_ALREADY_RESERVED");
        public Task<bool> DriverReservedAsync(Guid id, CancellationToken ct) => Task.FromResult(code == "DRIVER_ALREADY_RESERVED");
        public Task<IReadOnlyCollection<Guid>> ReservedVehiclesAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Guid>> ReservedDriversAsync(CancellationToken ct) => throw new NotSupportedException();
    }
}
