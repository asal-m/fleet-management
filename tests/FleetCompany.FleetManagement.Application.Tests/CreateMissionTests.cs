using MPCore.Persistence.Abstractions;
using MPCore.Domain.Rules;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Validators;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using Xunit;
namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class CreateMissionTests
{
    [Fact]
    public async Task Creation_stages_one_draft_with_normalized_locations_without_committing()
    {
        var repository = new Repository();
        var result = await new CreateMissionHandler(repository, new TestAuditor()).Handle(
            new CreateMission(" تهران ", " شیراز ", 1000), new NoCommit(), default);
        Assert.True(result.IsSuccess);
        var mission = Assert.Single(repository.Added);
        Assert.NotEqual(Guid.Empty, mission.Id);
        Assert.Equal("تهران", mission.Origin.Value);
        Assert.Equal("شیراز", mission.Destination.Value);
        Assert.Equal(1000, mission.RequiredCapacity.Kilograms);
        Assert.Equal(MissionStatus.Draft, mission.Status);
        Assert.Null(mission.ScheduledTime);
        Assert.Null(mission.AssignedVehicleId);
        Assert.Null(mission.AssignedDriverId);
        Assert.False(mission.HasActiveReservation);
    }
    [Fact]
    public async Task Invalid_domain_values_do_not_stage_partial_creation()
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() => new CreateMissionHandler(repository, new TestAuditor()).Handle(
            new CreateMission("A", "B", 0), new NoCommit(), default));
        Assert.Empty(repository.Added);
    }
    [Fact]
    public async Task Cancellation_before_execution_does_not_stage_a_mission()
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<OperationCanceledException>(() => new CreateMissionHandler(repository, new TestAuditor()).Handle(
            new CreateMission("A", "B", 1), new NoCommit(), new CancellationToken(true)));
        Assert.Empty(repository.Added);
    }
    [Fact]
    public void Validator_enforces_shape_and_accepts_equal_locations()
    {
        var validator = new CreateMissionValidator();
        Assert.Equal(3, validator.Validate(new CreateMission(null, " ", 0)).Errors.Count);
        Assert.False(validator.Validate(new CreateMission(new string('A', 501), "B", 1)).IsValid);
        Assert.True(validator.Validate(new CreateMission(" تهران ", "تهران", int.MaxValue)).IsValid);
        Assert.True(validator.Validate(new CreateMission(new string('A', 500), "B", 1)).IsValid);
    }
    private sealed class Repository : IMissionRepository
    {
        public Task<Mission?> FindAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public List<Mission> Added { get; } = [];
        public void Add(Mission mission) => Added.Add(mission);
    }
    private sealed class NoCommit : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Only middleware may commit.");
    }
}
