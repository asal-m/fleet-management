using Npgsql;
using MPCore.Application.Results;
using MPCore.Transport.Http;
using FleetCompany.FleetManagement.Modules.Fleet.Application;
using FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;
using Grpc.Core;
using MPCore.Transport.Grpc;
using FleetCompany.FleetManagement.Modules.Operations.Application;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;
using FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;

namespace FleetCompany.FleetManagement.Api.Rest;

public sealed class VehicleExceptionMapper : IHttpExceptionMapper, IGrpcExceptionMapper
{
    public FailureDescriptor? Map(Exception exception, HttpContext context) => Classify(exception);
    public FailureDescriptor? Map(Exception exception, ServerCallContext context) => Classify(exception);
    public static FailureDescriptor? Classify(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is not PostgresException postgres || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
                continue;
            return postgres.ConstraintName switch
            {
                VehicleConfiguration.PlateUniqueIndex => FleetFailures.DuplicatePlate(),
                MissionConfiguration.ActiveVehicleUniqueIndex => OperationsFailures.FromRule(new VehicleMustBeUnreservedRule(true)),
                MissionConfiguration.ActiveDriverUniqueIndex => OperationsFailures.FromRule(new DriverMustBeUnreservedRule(true)),
                _ => null
            };
        }

        return null;
    }
}
