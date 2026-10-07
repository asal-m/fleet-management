using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using MPCore.Domain.Rules;
using Xunit;

namespace FleetCompany.FleetManagement.Domain.Tests;

public sealed class VehicleTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Capacity_accepts_positive_boundaries(int kilograms)
        => Assert.Equal(kilograms, VehicleCapacity.Create(kilograms).Kilograms);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Capacity_rejects_nonpositive_values(int kilograms)
        => Assert.Throws<BusinessRuleValidationException>(() => VehicleCapacity.Create(kilograms));

    [Fact]
    public void Value_objects_compare_normalized_values()
    {
        Assert.Equal(VehicleCapacity.Create(1000), VehicleCapacity.Create(1000));
        Assert.Equal(VehicleTypeCode.Create(" truck "), VehicleTypeCode.Create("TRUCK"));
        Assert.Equal(PlateNumber.Create(" ab-۱۲۳ "), PlateNumber.Create("AB-123"));
        Assert.Equal(PlateNumber.Create("ab-١٢٣"), PlateNumber.Create("AB-123"));
        Assert.NotEqual(PlateNumber.Create("AB123"), PlateNumber.Create("AB-123"));
        Assert.Equal("الف 123", PlateNumber.Create(" الف ۱۲۳ ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TR UCK")]
    [InlineData("TR-UCK")]
    [InlineData("کامیون")]
    public void Type_code_rejects_invalid_input(string? value)
        => Assert.Throws<BusinessRuleValidationException>(() => VehicleTypeCode.Create(value));

    [Fact]
    public void Type_code_enforces_length_and_accepts_new_valid_codes()
    {
        Assert.Equal("A", VehicleTypeCode.Create("a").Value);
        Assert.Equal(50, VehicleTypeCode.Create(new string('A', 50)).Value.Length);
        Assert.Equal("NEW_TYPE_9", VehicleTypeCode.Create("new_type_9").Value);
        Assert.Throws<BusinessRuleValidationException>(() => VehicleTypeCode.Create(new string('A', 51)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Plate_rejects_empty_input(string? value)
        => Assert.Throws<BusinessRuleValidationException>(() => PlateNumber.Create(value));

    [Fact]
    public void Plate_enforces_normalized_length_boundaries()
    {
        Assert.Equal("A", PlateNumber.Create(" a ").Value);
        Assert.Equal(30, PlateNumber.Create(" " + new string('A', 30) + " ").Value.Length);
        Assert.Throws<BusinessRuleValidationException>(() => PlateNumber.Create(new string('A', 31)));
    }

    [Theory]
    [InlineData(VehicleBaseStatus.Active, VehicleOperationalStatus.Active)]
    [InlineData(VehicleBaseStatus.Inactive, VehicleOperationalStatus.Inactive)]
    public void Maintenance_preserves_base_status(VehicleBaseStatus baseStatus,
        VehicleOperationalStatus expectedAfterCompletion)
    {
        var vehicle = CreateVehicle(baseStatus);
        Assert.False(vehicle.IsUnderMaintenance);
        vehicle.StartMaintenance(hasActiveReservation: false);
        Assert.Equal(baseStatus, vehicle.BaseStatus);
        Assert.Equal(VehicleOperationalStatus.UnderMaintenance, vehicle.OperationalStatus);
        Assert.False(vehicle.IsOperationalForAssignment);
        vehicle.CompleteMaintenance();
        Assert.Equal(expectedAfterCompletion, vehicle.OperationalStatus);
        Assert.Equal(baseStatus == VehicleBaseStatus.Active, vehicle.IsOperationalForAssignment);
    }

    [Fact]
    public void Status_change_during_maintenance_takes_effect_after_completion()
    {
        var vehicle = CreateVehicle();
        vehicle.StartMaintenance(hasActiveReservation: false);
        vehicle.ChangeBaseStatus(VehicleBaseStatus.Inactive, hasActiveReservation: false);
        Assert.Equal(VehicleOperationalStatus.UnderMaintenance, vehicle.OperationalStatus);
        vehicle.ChangeBaseStatus(VehicleBaseStatus.Active, hasActiveReservation: false);
        Assert.False(vehicle.IsOperationalForAssignment);
        vehicle.ChangeBaseStatus(VehicleBaseStatus.Inactive, hasActiveReservation: false);
        vehicle.CompleteMaintenance();
        Assert.Equal(VehicleOperationalStatus.Inactive, vehicle.OperationalStatus);
    }

    [Fact]
    public void Starting_twice_is_rejected_without_changing_state()
    {
        var vehicle = CreateVehicle();
        vehicle.StartMaintenance(hasActiveReservation: false);
        Assert.Throws<BusinessRuleValidationException>(() => vehicle.StartMaintenance(hasActiveReservation: false));
        Assert.True(vehicle.IsUnderMaintenance);
        Assert.Equal(VehicleBaseStatus.Active, vehicle.BaseStatus);
    }

    [Fact]
    public void Completion_without_ongoing_maintenance_is_rejected()
    {
        var vehicle = CreateVehicle();
        Assert.Throws<BusinessRuleValidationException>(() => vehicle.CompleteMaintenance());
        vehicle.StartMaintenance(hasActiveReservation: false);
        vehicle.CompleteMaintenance();
        Assert.Throws<BusinessRuleValidationException>(() => vehicle.CompleteMaintenance());
        Assert.False(vehicle.IsUnderMaintenance);
        Assert.Equal(VehicleBaseStatus.Active, vehicle.BaseStatus);
    }

    [Fact]
    public void Invalid_base_status_is_rejected_before_mutation()
    {
        Assert.Throws<BusinessRuleValidationException>(() => CreateVehicle((VehicleBaseStatus)0));
        var vehicle = CreateVehicle();
        Assert.Throws<BusinessRuleValidationException>(() => vehicle.ChangeBaseStatus((VehicleBaseStatus)999, hasActiveReservation: false));
        Assert.Equal(VehicleBaseStatus.Active, vehicle.BaseStatus);
    }

    private static Vehicle CreateVehicle(VehicleBaseStatus status = VehicleBaseStatus.Active)
        => Vehicle.Register(Guid.NewGuid(), PlateNumber.Create("AB-123"),
            VehicleTypeCode.Create("TRUCK"), VehicleCapacity.Create(1000), status);
}
