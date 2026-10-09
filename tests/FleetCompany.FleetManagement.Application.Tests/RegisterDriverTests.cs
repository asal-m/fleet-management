using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Commands;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Ports;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Validators;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
using Xunit;

namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class RegisterDriverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData((DriverStatus)99)]
    public async Task Direct_handler_rejects_missing_or_unknown_status_without_staging(DriverStatus? status)
    {
        var repository = new Repository();
        var result = await new RegisterDriverHandler(repository).Handle(new RegisterDriver("A", "B", status, ["BUS"]), new NoCommit(), default);
        Assert.True(result.IsFailure);
        Assert.Equal("DRIVER_STATUS_INVALID", result.FailureDescriptor!.Identity.Code);
        Assert.Empty(repository.Added);
    }

    [Theory]
    [InlineData(DriverStatus.Active)]
    [InlineData(DriverStatus.Inactive)]
    public async Task Registration_stages_normalized_driver_without_manual_commit(DriverStatus status)
    {
        var repository = new Repository();
        var result = await new RegisterDriverHandler(repository).Handle(new RegisterDriver(" فاطمه ", " Mozafari ", status, [" truck ", "TRUCK", "bus"]), new NoCommit(), default);
        Assert.True(result.IsSuccess);
        var driver = Assert.Single(repository.Added);
        Assert.Equal("فاطمه", driver.FirstName.Value);
        Assert.Equal(status, driver.Status);
        Assert.Equal(2, driver.Qualifications.Count);
        Assert.True(driver.IsQualifiedFor(QualificationCode.Create("BUS")));
    }

    [Fact]
    public async Task Cancelled_request_does_not_stage_a_driver()
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<OperationCanceledException>(() => new RegisterDriverHandler(repository).Handle(new RegisterDriver("A", "B", DriverStatus.Active, ["BUS"]), new NoCommit(), new CancellationToken(true)));
        Assert.Empty(repository.Added);
    }

    [Fact]
    public void Validator_rejects_missing_status_names_and_qualifications()
    {
        var validator = new RegisterDriverValidator();
        Assert.False(validator.Validate(new RegisterDriver(" ", null, null, null)).IsValid);
        Assert.False(validator.Validate(new RegisterDriver("A", "B", DriverStatus.Active, [])).IsValid);
        Assert.False(validator.Validate(new RegisterDriver("A", "B", DriverStatus.Active, ["BUS", null])).IsValid);
        Assert.False(validator.Validate(new RegisterDriver("A", "B", (DriverStatus)3, ["BUS"])).IsValid);
        Assert.True(validator.Validate(new RegisterDriver("فاطمه", "B", DriverStatus.Inactive, [" bus ", "BUS"])).IsValid);
    }

    private sealed class Repository : IDriverRepository
    {
        public List<Driver> Added { get; } = [];

        public void Add(Driver driver) => Added.Add(driver);
    }

    private sealed class NoCommit : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Only middleware may commit.");
    }
}
