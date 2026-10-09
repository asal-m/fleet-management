using Wolverine;
using MPCore.Application.Results;
using MPCore.Security.AspNetCore;
using MPCore.Transport.Http;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Commands;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Queries;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Api.Rest.Endpoints;

public static class VehicleEndpoints
{
    public static RouteGroupBuilder MapVehicleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fleet/vehicles");
        group.MapGet("/available", async (int? limit, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<IReadOnlyList<VehicleDetailsView>>>(new GetAvailableVehicles(limit ?? 50), ct)).ToHttpResult(v => Results.Ok(v));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager", "Operator"));
        group.MapPut("/{id:guid}/status", async (Guid id, ChangeVehicleStatusRequest request, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<VehicleDetailsView>>(new ChangeVehicleStatus(id, request.Status), ct)).ToHttpResult(v => Results.Ok(v));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager"));
        group.MapPost("/{id:guid}/maintenance/start", async (Guid id, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<VehicleDetailsView>>(new StartMaintenance(id), ct)).ToHttpResult(v => Results.Ok(v));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager"));
        group.MapPost("/{id:guid}/maintenance/complete", async (Guid id, IMessageBus bus, CancellationToken ct) =>
        {
            return (await bus.InvokeAsync<Result<VehicleDetailsView>>(new CompleteMaintenance(id), ct)).ToHttpResult(v => Results.Ok(v));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager"));
        group.MapPost("/", async (RegisterVehicleRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var command = new RegisterVehicle(request.PlateNumber, request.TypeCode, request.CapacityKilograms, request.BaseStatus);
            var result = await bus.InvokeAsync<Result<RegisteredVehicleView>>(command, cancellationToken);
            return result.ToHttpResult(view => Results.Created($"/api/fleet/vehicles/{view.Id}", view));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager")).WithName("RegisterVehicle");
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var result = await bus.InvokeAsync<Result<VehicleDetailsView>>(new GetVehicle(id), cancellationToken);
            return result.ToHttpResult(view => Results.Ok(view));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager", "Operator")).WithName("GetVehicle");
        return group;
    }
}

public sealed record RegisterVehicleRequest(string? PlateNumber, string? TypeCode, int CapacityKilograms, VehicleBaseStatus? BaseStatus);
public sealed record ChangeVehicleStatusRequest(VehicleBaseStatus Status);
