using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
namespace FleetCompany.FleetManagement.Modules.Operations.Application.Views;

public static class MissionView { public static MissionDetailsView From(Mission m) => new(m.Id, m.Origin.Value, m.Destination.Value, m.RequiredCapacity.Kilograms, m.Status, m.ScheduledTime, m.AssignedVehicleId, m.AssignedDriverId); }
