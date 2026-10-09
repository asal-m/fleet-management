using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MPCore.Application.Results;
using MPCore.Transport.Grpc;
using FleetCompany.FleetManagement.Modules.Operations.Application;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions.Rules;
using FleetCompany.FleetManagement.Modules.Fleet.Application;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles.Rules;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class FailureTransportTests
{
    [Theory]
    [InlineData("capacity", StatusCode.FailedPrecondition, "INSUFFICIENT_VEHICLE_CAPACITY")]
    // MP Core 0.9.3 maps Conflict to Aborted; BusinessRule to FailedPrecondition.
    [InlineData("reservation", StatusCode.Aborted, "VEHICLE_ALREADY_RESERVED")]
    [InlineData("missing", StatusCode.NotFound, "VEHICLE_NOT_FOUND")]
    [InlineData("invalid", StatusCode.InvalidArgument, "MISSION_ID_INVALID")]
    [InlineData("maintenance", StatusCode.FailedPrecondition, "MAINTENANCE_NOT_STARTED")]
    [InlineData("commit_vehicle", StatusCode.Aborted, "VEHICLE_ALREADY_RESERVED")]
    [InlineData("commit_driver", StatusCode.Aborted, "DRIVER_ALREADY_RESERVED")]
    public async Task Installed_grpc_adapter_preserves_failure_category_and_rich_identity(string kind, StatusCode expected, string code)
    {
        // Adapter-only host: this probe is never mapped in the product API.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGrpc().AddMPCoreFailureHandling();
        builder.Services.AddSingleton<IGrpcExceptionMapper, FleetCompany.FleetManagement.Api.Rest.VehicleExceptionMapper>();
        await using var app = builder.Build();
        app.MapGrpcService<FailureProbeService>();
        await app.StartAsync();
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = app.GetTestServer().CreateHandler() });
        var call = channel.CreateCallInvoker().AsyncUnaryCall(FailureProbeService.Method, null, new CallOptions(), kind);
        using (call)
        {
            var error = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
            Assert.Equal(expected, error.StatusCode);
            var status = Google.Rpc.Status.Parser.ParseFrom(error.Trailers.Single(x => x.Key == "grpc-status-details-bin").ValueBytes);
            Assert.Equal((int)expected, status.Code);
            var info = status.Details.Single(x => x.Is(Google.Rpc.ErrorInfo.Descriptor)).Unpack<Google.Rpc.ErrorInfo>();
            Assert.Equal(kind == "maintenance" ? "fleet" : "operations", info.Domain);
            Assert.Equal(code, info.Reason);
        }
    }
}

[BindServiceMethod(typeof(FailureProbeService), nameof(BindService))]
public class FailureProbeService
{
    private static readonly Marshaller<string> Text = Marshallers.Create(value => Encoding.UTF8.GetBytes(value), bytes => Encoding.UTF8.GetString(bytes));
    public static readonly Method<string, string> Method = new(MethodType.Unary, "tests.FailureProbe", "Invoke", Text, Text);
    public static void BindService(ServiceBinderBase binder, FailureProbeService? service) => binder.AddMethod(Method, service is null ? null : new UnaryServerMethod<string, string>(service.Invoke));
    public virtual Task<string> Invoke(string kind, ServerCallContext context)
    {
        if (kind.StartsWith("commit_", StringComparison.Ordinal))
            throw new Microsoft.EntityFrameworkCore.DbUpdateException("private database details", new Npgsql.PostgresException("private database details", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.UniqueViolation, constraintName: kind == "commit_vehicle" ? "ux_active_mission_vehicle" : "ux_active_mission_driver"));
        var failure = kind switch
        {
            "capacity" => OperationsFailures.FromRule(new VehicleCapacityMustSatisfyMissionRule(999, 1000)),
            "reservation" => OperationsFailures.FromRule(new VehicleMustBeUnreservedRule(true)),
            "missing" => OperationsFailures.FromRule(new ResourceMustExistRule(false, "VEHICLE_NOT_FOUND", "vehicle_not_found")),
            "invalid" => OperationsFailures.InvalidMissionId(),
            "maintenance" => FleetFailures.FromRule(new MaintenanceMustBeInProgressRule(false)),
            _ => throw new ArgumentException("Unknown test case.")
        };
        throw new ResultFailureException(failure);
    }
}
