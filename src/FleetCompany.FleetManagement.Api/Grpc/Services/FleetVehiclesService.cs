using Grpc.Core;
using Wolverine;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Api.Grpc.Fleet;
using FleetCompany.FleetManagement.Modules.Fleet.Application;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using GetVehicleQuery = FleetCompany.FleetManagement.Modules.Fleet.Application.Queries.GetVehicle;
using GetAvailableVehiclesQuery = FleetCompany.FleetManagement.Modules.Fleet.Application.Queries.GetAvailableVehicles;
using ResultFailureException = MPCore.Application.Results.ResultFailureException;

namespace FleetCompany.FleetManagement.Api.Grpc.Services;

public sealed class FleetVehiclesService(IMessageBus bus) : FleetVehicles.FleetVehiclesBase
{
    public override async Task<GetAvailableVehiclesReply> GetAvailableVehicles(GetAvailableVehiclesRequest request, ServerCallContext context)
    {
        var result = await bus.InvokeAsync<Result<IReadOnlyList<VehicleDetailsView>>>(new GetAvailableVehiclesQuery(request.HasLimit ? request.Limit : 50), context.CancellationToken);
        result.ThrowIfFailure();
        var reply = new GetAvailableVehiclesReply();
        reply.Vehicles.AddRange(result.Value.Select(VehicleGrpcMapper.Map));
        return reply;
    }

    public override async Task<GetVehicleReply> GetVehicle(GetVehicleRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new ResultFailureException(FleetFailures.InvalidVehicleId());
        var result = await bus.InvokeAsync<Result<VehicleDetailsView>>(new GetVehicleQuery(id), context.CancellationToken);
        result.ThrowIfFailure();
        return new GetVehicleReply
        {
            Vehicle = VehicleGrpcMapper.Map(result.Value)
        };
    }
}
