using MPCore.Persistence.Abstractions;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Validators;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using Xunit;

namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class RegisterVehicleTests
{
    [Theory]
    [InlineData(VehicleBaseStatus.Active)]
    [InlineData(VehicleBaseStatus.Inactive)]
    public async Task Registration_normalizes_and_stages_one_vehicle_without_committing(VehicleBaseStatus status)
    {
        var repository = new FakeRepository();
        var result = await new RegisterVehicleHandler(repository, new TestCoordinator()).Handle(
            new RegisterVehicle(" ab-۱۲۳ ", " truck ", 1000, status), new NoCommitUnitOfWork(), default);
        Assert.True(result.IsSuccess);
        var vehicle = Assert.Single(repository.Added);
        Assert.NotEqual(Guid.Empty, vehicle.Id);
        Assert.Equal("AB-123", vehicle.PlateNumber.Value);
        Assert.Equal("TRUCK", vehicle.TypeCode.Value);
        Assert.Equal(status, vehicle.BaseStatus);
        Assert.False(vehicle.IsUnderMaintenance);
    }

    [Fact]
    public async Task Duplicate_plate_returns_conflict_without_staging_vehicle()
    {
        var repository = new FakeRepository { Exists = true };
        var result = await new RegisterVehicleHandler(repository, new TestCoordinator()).Handle(
            new RegisterVehicle("AB-123", "TRUCK", 1000, VehicleBaseStatus.Active), new NoCommitUnitOfWork(), default);
        Assert.True(result.IsFailure);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public void Validator_reports_missing_status_and_invalid_fields()
    {
        var result = new RegisterVehicleValidator().Validate(new RegisterVehicle(" ", "BAD-TYPE", 0, null));
        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Count);
    }

    [Theory]
    [InlineData(VehicleBaseStatus.Active)]
    [InlineData(VehicleBaseStatus.Inactive)]
    public void Validator_accepts_explicit_valid_status(VehicleBaseStatus status)
        => Assert.True(new RegisterVehicleValidator().Validate(new RegisterVehicle("AB-۱۲۳", " truck ", 1, status)).IsValid);

    private sealed class FakeRepository : IVehicleRepository
    {
        public Task<Vehicle?> FindAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public bool Exists { get; init; }
        public List<Vehicle> Added { get; } = [];
        public Task<bool> PlateExistsAsync(PlateNumber plateNumber, CancellationToken cancellationToken)
            => Task.FromResult(Exists);
        public void Add(Vehicle vehicle) => Added.Add(vehicle);
    }

    private sealed class NoCommitUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Handler must not commit; transaction middleware owns commit.");
    }
}
