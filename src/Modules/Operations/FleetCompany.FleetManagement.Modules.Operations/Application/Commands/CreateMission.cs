using MPCore.Application.Messaging;
using MPCore.Application.Results;
using MPCore.Persistence.Abstractions;
using MPCore.Audit;
using FleetCompany.FleetManagement.Modules.Operations.Application.Ports;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Modules.Operations.Application.Commands;

public sealed record CreateMission(string? Origin, string? Destination, int RequiredCapacityKilograms) : ICommand<Result<CreatedMissionView>>
{
    public override string ToString() => nameof(CreateMission);
}

public sealed class CreateMissionHandler(IMissionRepository repository, IBusinessAuditRecorder audit)
{
    public async Task<Result<CreatedMissionView>> Handle(CreateMission command, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = unitOfWork; // Middleware owns the transaction and commit.
        var mission = Mission.Create(Guid.CreateVersion7(), MissionLocation.Create(command.Origin), MissionLocation.Create(command.Destination), RequiredCapacity.Create(command.RequiredCapacityKilograms));
        repository.Add(mission);
        await audit.RecordAsync("operations", "MissionCreated", nameof(Mission), mission.Id.ToString(), null, cancellationToken);
        return Result<CreatedMissionView>.Success(new(mission.Id, mission.Origin.Value, mission.Destination.Value, mission.RequiredCapacity.Kilograms, mission.Status, mission.ScheduledTime, mission.AssignedVehicleId, mission.AssignedDriverId));
    }
}
