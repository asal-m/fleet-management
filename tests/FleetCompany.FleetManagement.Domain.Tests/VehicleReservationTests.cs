using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using MPCore.Domain.Rules;
using Xunit;

namespace FleetCompany.FleetManagement.Domain.Tests;

public sealed class VehicleReservationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Trusted_reservation_facts_prevent_maintenance_or_inactivation_before_mutation(bool maintenance)
    {
        var vehicle = Vehicle.Register(Guid.NewGuid(), PlateNumber.Create("RESERVED"), VehicleTypeCode.Create("TRUCK"), VehicleCapacity.Create(1000), VehicleBaseStatus.Active);
        var error = Assert.Throws<BusinessRuleValidationException>(() =>
        {
            if (maintenance) vehicle.StartMaintenance(hasActiveReservation: true);
            else vehicle.ChangeBaseStatus(VehicleBaseStatus.Inactive, hasActiveReservation: true);
        });
        Assert.Equal("VEHICLE_HAS_ACTIVE_MISSION", error.Rule.Code);
        Assert.Equal(VehicleBaseStatus.Active, vehicle.BaseStatus);
        Assert.False(vehicle.IsUnderMaintenance);
    }
}
