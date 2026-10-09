using Grpc.Core;
using Wolverine;
using MPCore.Application.Results;
using Google.Protobuf.WellKnownTypes;
using FleetCompany.FleetManagement.Api.Grpc.Operations;
using FleetCompany.FleetManagement.Modules.Operations.Application;
using FleetCompany.FleetManagement.Modules.Operations.Application.Queries;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Api.Grpc.Services;

public sealed class OperationsMissionsService(IMessageBus bus) : OperationsMissions.OperationsMissionsBase
{
    public override async Task<GetMissionReply> GetMission(GetMissionRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new MPCore.Application.Results.ResultFailureException(OperationsFailures.InvalidMissionId());
        var result = await bus.InvokeAsync<Result<MissionDetailsView>>(new GetMission(id), context.CancellationToken);
        result.ThrowIfFailure();
        return new()
        {
            Mission = Map(result.Value)
        };
    }

    public override async Task<GetActiveMissionsReply> GetActiveMissions(GetActiveMissionsRequest request, ServerCallContext context)
    {
        var result = await bus.InvokeAsync<Result<IReadOnlyList<MissionDetailsView>>>(new GetActiveMissions(request.HasLimit ? request.Limit : 50), context.CancellationToken);
        result.ThrowIfFailure();
        var reply = new GetActiveMissionsReply();
        reply.Missions.AddRange(result.Value.Select(Map));
        return reply;
    }

    private static MissionDetails Map(MissionDetailsView view)
    {
        var details = new MissionDetails
        {
            Id = view.Id.ToString(),
            Origin = view.Origin,
            Destination = view.Destination,
            RequiredCapacityKilograms = view.RequiredCapacityKilograms,
            Status = view.Status switch
            {
                MissionStatus.Draft => MissionState.Draft,
                MissionStatus.Scheduled => MissionState.Scheduled,
                MissionStatus.Assigned => MissionState.Assigned,
                MissionStatus.InProgress => MissionState.InProgress,
                MissionStatus.Completed => MissionState.Completed,
                MissionStatus.Cancelled => MissionState.Cancelled,
                _ => throw new InvalidOperationException("Unexpected stored Mission status.")
            }
        };
        if (view.ScheduledTime.HasValue)
            details.ScheduledTime = Timestamp.FromDateTimeOffset(view.ScheduledTime.Value);
        if (view.AssignedVehicleId.HasValue)
            details.AssignedVehicleId = view.AssignedVehicleId.Value.ToString();
        if (view.AssignedDriverId.HasValue)
            details.AssignedDriverId = view.AssignedDriverId.Value.ToString();
        return details;
    }
}
