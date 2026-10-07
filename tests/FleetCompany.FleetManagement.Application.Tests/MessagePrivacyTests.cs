using FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Commands;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;
using Xunit;

namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class MessagePrivacyTests
{
    [Fact]
    public void Message_logging_does_not_expand_names_plates_or_addresses()
    {
        Assert.Equal(nameof(RegisterVehicle), new RegisterVehicle("PRIVATE-PLATE", "TRUCK", 1000, VehicleBaseStatus.Active).ToString());
        Assert.Equal(nameof(RegisterDriver), new RegisterDriver("PRIVATE-FIRST", "PRIVATE-LAST", DriverStatus.Active, ["TRUCK"]).ToString());
        Assert.Equal(nameof(CreateMission), new CreateMission("PRIVATE-ORIGIN", "PRIVATE-DESTINATION", 1000).ToString());
    }
}
