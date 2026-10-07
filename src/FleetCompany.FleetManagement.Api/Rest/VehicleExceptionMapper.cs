using Npgsql;
using MPCore.Application.Results;
using MPCore.Transport.Http;
using FleetCompany.FleetManagement.Modules.Fleet.Application;
using FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;

namespace FleetCompany.FleetManagement.Api.Rest;

public sealed class VehicleExceptionMapper : IHttpExceptionMapper
{
    public FailureDescriptor? Map(Exception exception, HttpContext context)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && postgres.ConstraintName == VehicleConfiguration.PlateUniqueIndex)
                return FleetFailures.DuplicatePlate();
        }

        return null;
    }
}
