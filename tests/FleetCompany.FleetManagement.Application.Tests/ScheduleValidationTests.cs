using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;
using FleetCompany.FleetManagement.Modules.Operations.Application.Validators;
using MPCore.Persistence.Abstractions;
using Xunit;

namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class ScheduleValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("2026-10-10T12:00:00")]
    [InlineData("2026-10-10T12:00:00+99:00")]
    public async Task Invalid_time_is_rejected_by_validator_and_direct_handler_before_workflow(string? time)
    {
        var command = new ScheduleMission(Guid.NewGuid(), time);
        var validation = new ScheduleMissionValidator().Validate(command);
        var violation = Assert.Single(validation.Errors);
        Assert.Equal("ScheduledTime", violation.PropertyName);
        Assert.Equal("SCHEDULED_TIME_INVALID", violation.ErrorCode);
        // A null workflow makes accidental dispatch observable; invalid input must return first.
        var result = await new ScheduleMissionHandler(null!, TimeProvider.System).Handle(command, new NoCommit(), default);
        Assert.True(result.IsFailure);
        Assert.Equal("SCHEDULED_TIME_INVALID", result.FailureDescriptor!.Identity.Code);
    }

    [Theory]
    [InlineData("2026-10-10T12:00:00Z")]
    [InlineData("2026-10-10T15:30:00+03:30")]
    public void Explicit_offset_represents_the_same_instant(string time)
    {
        Assert.True(new ScheduleMissionValidator().Validate(new ScheduleMission(Guid.NewGuid(), time)).IsValid);
        Assert.True(ScheduledTimestamp.TryParse(time, out var parsed));
        Assert.Equal(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero), parsed);
    }

    private sealed class NoCommit : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Only middleware may commit.");
    }
}
