using Wolverine;
using MPCore.Application.Results;
using MPCore.Security.AspNetCore;
using MPCore.Transport.Http;
using FleetCompany.FleetManagement.Modules.Operations.Application.Commands;
using FleetCompany.FleetManagement.Modules.Operations.Application.Queries;
using FleetCompany.FleetManagement.Modules.Operations.Application.Views;

namespace FleetCompany.FleetManagement.Api.Rest.Endpoints;

public static class MissionEndpoints
{
    public static RouteGroupBuilder MapMissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/operations/missions");
        group.RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("Operator"));
        group.MapGet("/active", async (int? limit, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<IReadOnlyList<MissionDetailsView>>>(new GetActiveMissions(limit ?? 50), ct)).ToHttpResult(v => Results.Ok(v));
        });
        group.MapPost("/{id:guid}/schedule", async (Guid id, ScheduleMissionRequest request, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<MissionDetailsView>>(new ScheduleMission(id, request.ScheduledTime), ct)).ToHttpResult(v => Results.Ok(v));
        });
        group.MapPost("/{id:guid}/assign", async (Guid id, AssignMissionRequest request, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<MissionDetailsView>>(new AssignMission(id, request.VehicleId, request.DriverId), ct)).ToHttpResult(v => Results.Ok(v));
        });
        group.MapPost("/{id:guid}/start", async (Guid id, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<MissionDetailsView>>(new StartMission(id), ct)).ToHttpResult(v => Results.Ok(v));
        });
        group.MapPost("/{id:guid}/complete", async (Guid id, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<MissionDetailsView>>(new CompleteMission(id), ct)).ToHttpResult(v => Results.Ok(v));
        });
        group.MapPost("/{id:guid}/cancel", async (Guid id, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<MissionDetailsView>>(new CancelMission(id), ct)).ToHttpResult(v => Results.Ok(v));
        });
        group.MapPost("/", async (CreateMissionRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var command = new CreateMission(request.Origin, request.Destination, request.RequiredCapacityKilograms);
            var result = await bus.InvokeAsync<Result<CreatedMissionView>>(command, cancellationToken);
            return result.ToHttpResult(view => Results.Created($"/api/operations/missions/{view.Id}", view));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("Operator")).WithName("CreateMission");
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var result = await bus.InvokeAsync<Result<MissionDetailsView>>(new GetMission(id), cancellationToken);
            return result.ToHttpResult(view => Results.Ok(view));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("Operator")).WithName("GetMission");
        return group;
    }
}

public sealed record CreateMissionRequest(string? Origin, string? Destination, int RequiredCapacityKilograms);
public sealed record ScheduleMissionRequest(string? ScheduledTime);
public sealed record AssignMissionRequest(Guid VehicleId, Guid DriverId);
