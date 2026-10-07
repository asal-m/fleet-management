using Wolverine;
using MPCore.Application.Results;
using MPCore.Audit;
using MPCore.Security.AspNetCore;
using MPCore.Transport.Http;
using FleetCompany.FleetManagement.Modules.Administration.Application.Queries;
namespace FleetCompany.FleetManagement.Api.Rest.Endpoints;

public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/administration/audit").RequireAuthorization(MPCoreAuthorizationPolicies.RequireRole("Administrator"));
        group.MapGet("/", async (string? entityId, int? page, int? size, IMessageBus bus, CancellationToken ct) =>
         (await bus.InvokeAsync<Result<AuditPage>>(new GetAudit(entityId, page ?? 1, size ?? 50), ct)).ToHttpResult(v => Results.Ok(v)));
        return group;
    }
}
