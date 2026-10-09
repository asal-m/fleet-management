using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Views;

public sealed record CreatedMissionView(Guid Id, string Origin, string Destination, int RequiredCapacityKilograms, MissionStatus Status, DateTimeOffset? ScheduledTime, Guid? AssignedVehicleId, Guid? AssignedDriverId)
{
    public string StatusName => Status.ToString();
}
