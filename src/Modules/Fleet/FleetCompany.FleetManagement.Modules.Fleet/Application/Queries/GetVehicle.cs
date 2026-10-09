using MPCore.Application.Messaging;
using MPCore.Application.Results;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Ports;
using FleetCompany.FleetManagement.Modules.Fleet.Application.Views;

namespace FleetCompany.FleetManagement.Modules.Fleet.Application.Queries;

public sealed record GetVehicle(Guid Id) : IQuery<Result<VehicleDetailsView>>;
public sealed class GetVehicleHandler(IVehicleReadModel readModel)
{
    public async Task<Result<VehicleDetailsView>> Handle(GetVehicle query, CancellationToken cancellationToken)
    {
        var vehicle = await readModel.GetAsync(query.Id, cancellationToken);
        return vehicle is null ? Result<VehicleDetailsView>.FromFailure(FleetFailures.VehicleNotFound()) : Result<VehicleDetailsView>.Success(vehicle);
    }
}
