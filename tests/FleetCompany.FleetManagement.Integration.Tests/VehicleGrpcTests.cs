using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Api.Grpc.Fleet;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class VehicleGrpcTests
{
    [HttpDependenciesFact]
    public async Task Grpc_reuses_vehicle_query_and_preserves_failure_and_authorization_behavior()
    {
        await using var factory = new FleetTestHost();
        using var http = factory.CreateClient();
        using var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = factory.Server.CreateHandler()
        });
        var grpc = new FleetVehicles.FleetVehiclesClient(channel);
        var plate = "GRPC-" + Guid.NewGuid().ToString("N")[..20];
        var manager = Headers(factory.Token("FleetManager"));
        var operatorHeaders = Headers(factory.Token("Operator"));
        try
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("FleetManager"));
            using var created = await http.PostAsJsonAsync("/api/fleet/vehicles/", new
            {
                plateNumber = plate,
                typeCode = "TRUCK",
                capacityKilograms = 1000,
                baseStatus = 1
            });
            Assert.Equal(System.Net.HttpStatusCode.Created, created.StatusCode);
            using var rest = await http.GetAsync(created.Headers.Location);
            rest.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await rest.Content.ReadAsStringAsync());
            var id = body.RootElement.GetProperty("id").GetString()!;

            var reply = await grpc.GetVehicleAsync(new GetVehicleRequest { Id = id }, manager).ResponseAsync;
            Assert.Equal(id, reply.Vehicle.Id);
            Assert.Equal(body.RootElement.GetProperty("plateNumber").GetString(), reply.Vehicle.PlateNumber);
            Assert.Equal(body.RootElement.GetProperty("typeCode").GetString(), reply.Vehicle.TypeCode);
            Assert.Equal(body.RootElement.GetProperty("capacityKilograms").GetInt32(), reply.Vehicle.CapacityKilograms);
            Assert.Equal(body.RootElement.GetProperty("baseStatus").GetInt32(), (int)reply.Vehicle.BaseStatus);
            Assert.Equal(body.RootElement.GetProperty("operationalStatus").GetInt32(), (int)reply.Vehicle.OperationalStatus);
            Assert.Equal(body.RootElement.GetProperty("isUnderMaintenance").GetBoolean(), reply.Vehicle.IsUnderMaintenance);
            Assert.Equal(id, (await grpc.GetVehicleAsync(new GetVehicleRequest { Id = id }, operatorHeaders).ResponseAsync).Vehicle.Id);

            var anonymous = await Assert.ThrowsAsync<RpcException>(() => grpc.GetVehicleAsync(new GetVehicleRequest { Id = id }).ResponseAsync);
            Assert.Equal(StatusCode.Unauthenticated, anonymous.StatusCode);
            var forbidden = await Assert.ThrowsAsync<RpcException>(() => grpc.GetVehicleAsync(new GetVehicleRequest { Id = id }, Headers(factory.Token("Administrator"))).ResponseAsync);
            Assert.Equal(StatusCode.PermissionDenied, forbidden.StatusCode);

            var missingId = Guid.NewGuid().ToString();
            using var restMissing = await http.GetAsync($"/api/fleet/vehicles/{missingId}");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, restMissing.StatusCode);
            Assert.Contains("VEHICLE_NOT_FOUND", await restMissing.Content.ReadAsStringAsync());
            var missing = await Assert.ThrowsAsync<RpcException>(() => grpc.GetVehicleAsync(new GetVehicleRequest { Id = missingId }, manager).ResponseAsync);
            Assert.Equal(StatusCode.NotFound, missing.StatusCode);
            AssertRichIdentity(missing, "VEHICLE_NOT_FOUND");

            var invalid = await Assert.ThrowsAsync<RpcException>(() => grpc.GetVehicleAsync(new GetVehicleRequest { Id = "not-a-guid" }, manager).ResponseAsync);
            Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
            AssertRichIdentity(invalid, "VEHICLE_ID_INVALID");
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var normalized = PlateNumber.Create(plate);
            await database.Set<Vehicle>().Where(vehicle => vehicle.PlateNumber == normalized).ExecuteDeleteAsync();
        }
    }

    private static Metadata Headers(string token) => new() { { "authorization", "Bearer " + token } };

    private static void AssertRichIdentity(RpcException exception, string expectedCode)
    {
        var metadata = exception.Trailers.Single(entry => entry.Key == "grpc-status-details-bin");
        var richStatus = Google.Rpc.Status.Parser.ParseFrom(metadata.ValueBytes);
        var identity = richStatus.Details.Single(detail => detail.Is(Google.Rpc.ErrorInfo.Descriptor)).Unpack<Google.Rpc.ErrorInfo>();
        Assert.Equal("fleet", identity.Domain);
        Assert.Equal(expectedCode, identity.Reason);
    }
}
