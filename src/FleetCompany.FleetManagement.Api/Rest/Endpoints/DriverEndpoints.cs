using Wolverine;
using MPCore.Application.Results;
using MPCore.Security.AspNetCore;
using MPCore.Transport.Http;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Commands;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Views;
using FleetCompany.FleetManagement.Modules.Drivers.Application.Queries;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
namespace FleetCompany.FleetManagement.Api.Rest.Endpoints;

public static class DriverEndpoints
{
    public static RouteGroupBuilder MapDriverEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/drivers");
        group.MapGet("/available", async (int? limit, IMessageBus bus, CancellationToken ct) =>
            (await bus.InvokeAsync<Result<IReadOnlyList<RegisteredDriverView>>>(new GetAvailableDrivers(limit ?? 50), ct)).ToHttpResult(v => Results.Ok(v)))
            .RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager", "Operator"));
        group.MapPost("/", async (RegisterDriverRequest request, IMessageBus bus, CancellationToken cancellationToken) =>
        {
            var command = new RegisterDriver(request.FirstName, request.LastName, request.Status, request.Qualifications);
            var result = await bus.InvokeAsync<Result<RegisteredDriverView>>(command, cancellationToken);
            return result.ToHttpResult(view => Results.Created((string?)null, view));
        }).RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("FleetManager")).WithName("RegisterDriver");
        return group;
    }
}
public sealed record RegisterDriverRequest(string? FirstName, string? LastName, DriverStatus? Status, IReadOnlyList<string?>? Qualifications);
