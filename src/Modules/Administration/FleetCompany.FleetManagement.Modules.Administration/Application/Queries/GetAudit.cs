using MPCore.Audit;
using MPCore.Application.Messaging;
using MPCore.Application.Results;
namespace FleetCompany.FleetManagement.Modules.Administration.Application.Queries;

public sealed record GetAudit(string? EntityId, int Page = 1, int Size = 50) : IQuery<Result<AuditPage>>;
public sealed class GetAuditHandler(IAuditQuery query)
{
    public async Task<Result<AuditPage>> Handle(GetAudit request, CancellationToken ct)
    => Result<AuditPage>.Success(await query.QueryAsync(new AuditQueryFilter { EntityId = request.EntityId }, new AuditPageRequest(request.Page, request.Size), ct));
}
