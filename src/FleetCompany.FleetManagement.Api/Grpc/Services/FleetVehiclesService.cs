using Grpc.Core;
using Wolverine;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Api.Grpc.Fleet;
using FleetCompany.FleetManagement.Modules.Fleet.Application;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using GetVehicleQuery = FleetCompany.FleetManagement.Modules.Fleet.Application.Queries.GetVehicle;

namespace FleetCompany.FleetManagement.Api.Grpc.Services;

public sealed class FleetVehiclesService(IMessageBus bus) : FleetVehicles.FleetVehiclesBase
{
    public override async Task<GetAvailableVehiclesReply> GetAvailableVehicles(GetAvailableVehiclesRequest request, ServerCallContext context)
    {
        var result = await bus.InvokeAsync<Result<IReadOnlyList<VehicleDetailsView>>>(new FleetCompany.FleetManagement.Modules.Fleet.Application.Queries.GetAvailableVehicles(request.HasLimit ? request.Limit : 50), context.CancellationToken);
        result.ThrowIfFailure(); var reply = new GetAvailableVehiclesReply();
        reply.Vehicles.AddRange(result.Value.Select(v => new VehicleDetails { Id = v.Id.ToString(), PlateNumber = v.PlateNumber, TypeCode = v.TypeCode, CapacityKilograms = v.CapacityKilograms, BaseStatus = VehicleBaseState.Active, OperationalStatus = VehicleOperationalState.Active, IsUnderMaintenance = false }));
        return reply;
    }
    public override async Task<GetVehicleReply> GetVehicle(GetVehicleRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new MPCore.Application.Results.ResultFailureException(FleetFailures.InvalidVehicleId());

        var result = await bus.InvokeAsync<Result<VehicleDetailsView>>(new GetVehicleQuery(id), context.CancellationToken);
        result.ThrowIfFailure();
        var view = result.Value;
        return new GetVehicleReply
        {
            Vehicle = new VehicleDetails
            {
                Id = view.Id.ToString(),
                PlateNumber = view.PlateNumber,
                TypeCode = view.TypeCode,
                CapacityKilograms = view.CapacityKilograms,
                BaseStatus = view.BaseStatus switch
                {
                    VehicleBaseStatus.Active => VehicleBaseState.Active,
                    VehicleBaseStatus.Inactive => VehicleBaseState.Inactive,
                    _ => throw new InvalidOperationException("Unexpected stored vehicle base status.")
                },
                OperationalStatus = view.OperationalStatus switch
                {
                    VehicleOperationalStatus.Active => VehicleOperationalState.Active,
                    VehicleOperationalStatus.Inactive => VehicleOperationalState.Inactive,
                    VehicleOperationalStatus.UnderMaintenance => VehicleOperationalState.UnderMaintenance,
                    _ => throw new InvalidOperationException("Unexpected vehicle operational status.")
                },
                IsUnderMaintenance = view.IsUnderMaintenance
            }
        };
    }
}
