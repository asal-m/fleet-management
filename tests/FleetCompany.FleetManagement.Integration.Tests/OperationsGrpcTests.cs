using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Api.Grpc.Operations;
using FleetCompany.FleetManagement.Api.Grpc.Fleet;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using Xunit;
namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class OperationsGrpcTests
{
    [HttpDependenciesFact]
    public async Task Required_grpc_queries_reuse_rest_views_and_failures_with_real_bearer_validation()
    {
        await using var host = new FleetTestHost(); using var http = host.CreateClient();
        using var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions { HttpHandler = host.Server.CreateHandler() });
        var grpc = new OperationsMissions.OperationsMissionsClient(channel);
        var fleetGrpc = new FleetVehicles.FleetVehiclesClient(channel);
        var operatorToken = host.Token("Operator"); var headers = new Metadata { { "authorization", "Bearer " + operatorToken } };
        http.DefaultRequestHeaders.Authorization = new("Bearer", operatorToken);
        Guid? missionId = null, vehicleId = null, reservedVehicleId = null, reservedDriverId = null;
        try
        {
            using var created = await http.PostAsJsonAsync("/api/operations/missions/", new { origin = "GrpcOrigin", destination = "GrpcDestination", requiredCapacityKilograms = 1000 });
            created.EnsureSuccessStatusCode(); using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync()); missionId = body.RootElement.GetProperty("id").GetGuid();
            var draft = await grpc.GetMissionAsync(new GetMissionRequest { Id = missionId.ToString() }, headers).ResponseAsync;
            Assert.Equal(MissionState.Draft, draft.Mission.Status); Assert.Null(draft.Mission.ScheduledTime);
            Assert.False(draft.Mission.HasAssignedVehicleId); Assert.False(draft.Mission.HasAssignedDriverId);
            using (var scheduled = await http.PostAsJsonAsync($"/api/operations/missions/{missionId}/schedule", new { scheduledTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O") })) scheduled.EnsureSuccessStatusCode();
            using var rest = await http.GetAsync($"/api/operations/missions/{missionId}"); rest.EnsureSuccessStatusCode();
            using var representation = JsonDocument.Parse(await rest.Content.ReadAsStringAsync());
            var read = await grpc.GetMissionAsync(new GetMissionRequest { Id = missionId.ToString() }, headers).ResponseAsync;
            Assert.Equal(representation.RootElement.GetProperty("id").GetString(), read.Mission.Id);
            Assert.Equal(representation.RootElement.GetProperty("origin").GetString(), read.Mission.Origin);
            Assert.Equal(representation.RootElement.GetProperty("destination").GetString(), read.Mission.Destination);
            Assert.Equal(representation.RootElement.GetProperty("requiredCapacityKilograms").GetInt32(), read.Mission.RequiredCapacityKilograms);
            Assert.Equal(representation.RootElement.GetProperty("status").GetInt32(), (int)read.Mission.Status);
            Assert.Equal(representation.RootElement.GetProperty("scheduledTime").GetDateTimeOffset(), read.Mission.ScheduledTime.ToDateTimeOffset());
            http.DefaultRequestHeaders.Authorization = new("Bearer", host.Token("FleetManager"));
            using (var v = await http.PostAsJsonAsync("/api/fleet/vehicles/", new { plateNumber = "RPC-" + Guid.NewGuid().ToString("N")[..20], typeCode = "TRUCK", capacityKilograms = 2000, baseStatus = 1 }))
            {
                v.EnsureSuccessStatusCode(); using var vbody = JsonDocument.Parse(await v.Content.ReadAsStringAsync()); reservedVehicleId = vbody.RootElement.GetProperty("id").GetGuid();
            }
            using (var d = await http.PostAsJsonAsync("/api/drivers/", new { firstName = "Grpc", lastName = "Driver", status = 1, qualifications = new[] { "TRUCK" } }))
            {
                d.EnsureSuccessStatusCode(); using var dbody = JsonDocument.Parse(await d.Content.ReadAsStringAsync()); reservedDriverId = dbody.RootElement.GetProperty("id").GetGuid();
            }
            http.DefaultRequestHeaders.Authorization = new("Bearer", operatorToken);
            using (var assigned = await http.PostAsJsonAsync($"/api/operations/missions/{missionId}/assign", new { vehicleId = reservedVehicleId, driverId = reservedDriverId })) assigned.EnsureSuccessStatusCode();
            using var activeRest = await http.GetAsync("/api/operations/missions/active?limit=200"); activeRest.EnsureSuccessStatusCode();
            using var activeBody = JsonDocument.Parse(await activeRest.Content.ReadAsStringAsync());
            Assert.Contains(activeBody.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == missionId);
            var active = await grpc.GetActiveMissionsAsync(new GetActiveMissionsRequest { Limit = 200 }, headers).ResponseAsync;
            Assert.Contains(active.Missions, x => x.Id == missionId.ToString() && x.HasAssignedVehicleId && x.HasAssignedDriverId && x.Status == MissionState.Assigned);
            var anonymous = await Assert.ThrowsAsync<RpcException>(() => grpc.GetMissionAsync(new GetMissionRequest { Id = missionId.ToString() }).ResponseAsync);
            Assert.Equal(StatusCode.Unauthenticated, anonymous.StatusCode);
            var denied = await Assert.ThrowsAsync<RpcException>(() => grpc.GetMissionAsync(new GetMissionRequest { Id = missionId.ToString() }, new Metadata { { "authorization", "Bearer " + host.Token("FleetManager") } }).ResponseAsync);
            Assert.Equal(StatusCode.PermissionDenied, denied.StatusCode);
            var missingId = Guid.NewGuid().ToString();
            using var missingRest = await http.GetAsync("/api/operations/missions/" + missingId);
            Assert.Equal(HttpStatusCode.NotFound, missingRest.StatusCode); Assert.Contains("MISSION_NOT_FOUND", await missingRest.Content.ReadAsStringAsync());
            var missing = await Assert.ThrowsAsync<RpcException>(() => grpc.GetMissionAsync(new GetMissionRequest { Id = missingId }, headers).ResponseAsync);
            Assert.Equal(StatusCode.NotFound, missing.StatusCode); AssertIdentity(missing, "MISSION_NOT_FOUND");
            var invalid = await Assert.ThrowsAsync<RpcException>(() => grpc.GetMissionAsync(new GetMissionRequest { Id = "bad" }, headers).ResponseAsync);
            Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode); AssertIdentity(invalid, "MISSION_ID_INVALID");
            var invalidLimit = await Assert.ThrowsAsync<RpcException>(() => grpc.GetActiveMissionsAsync(new GetActiveMissionsRequest { Limit = 0 }, headers).ResponseAsync);
            Assert.Equal(StatusCode.InvalidArgument, invalidLimit.StatusCode);
            http.DefaultRequestHeaders.Authorization = new("Bearer", host.Token("FleetManager"));
            using var registered = await http.PostAsJsonAsync("/api/fleet/vehicles/", new { plateNumber = "GRPC-" + Guid.NewGuid().ToString("N")[..20], typeCode = "BUS", capacityKilograms = 1500, baseStatus = 1 });
            registered.EnsureSuccessStatusCode(); using var vehicleBody = JsonDocument.Parse(await registered.Content.ReadAsStringAsync()); vehicleId = vehicleBody.RootElement.GetProperty("id").GetGuid();
            http.DefaultRequestHeaders.Authorization = new("Bearer", operatorToken);
            using var availableRest = await http.GetAsync("/api/fleet/vehicles/available?limit=200"); availableRest.EnsureSuccessStatusCode();
            using var availableBody = JsonDocument.Parse(await availableRest.Content.ReadAsStringAsync());
            var restVehicle = availableBody.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == vehicleId);
            var available = await fleetGrpc.GetAvailableVehiclesAsync(new GetAvailableVehiclesRequest { Limit = 200 }, headers).ResponseAsync;
            var grpcVehicle = available.Vehicles.Single(x => x.Id == vehicleId.ToString());
            Assert.Equal(restVehicle.GetProperty("plateNumber").GetString(), grpcVehicle.PlateNumber);
            Assert.Equal(restVehicle.GetProperty("typeCode").GetString(), grpcVehicle.TypeCode);
            Assert.Equal(restVehicle.GetProperty("capacityKilograms").GetInt32(), grpcVehicle.CapacityKilograms);
            Assert.Equal(restVehicle.GetProperty("operationalStatus").GetInt32(), (int)grpcVehicle.OperationalStatus);
        }
        finally
        {
            using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (missionId.HasValue) await db.Set<Mission>().Where(x => x.Id == missionId).ExecuteDeleteAsync();
            if (vehicleId.HasValue) await db.Set<Vehicle>().Where(x => x.Id == vehicleId).ExecuteDeleteAsync();
            if (reservedVehicleId.HasValue) await db.Set<Vehicle>().Where(x => x.Id == reservedVehicleId).ExecuteDeleteAsync();
            if (reservedDriverId.HasValue) await db.Set<FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers.Driver>().Where(x => x.Id == reservedDriverId).ExecuteDeleteAsync();
        }
    }
    private static void AssertIdentity(RpcException error, string code)
    {
        var value = error.Trailers.Single(x => x.Key == "grpc-status-details-bin");
        var status = Google.Rpc.Status.Parser.ParseFrom(value.ValueBytes);
        var info = status.Details.Single(x => x.Is(Google.Rpc.ErrorInfo.Descriptor)).Unpack<Google.Rpc.ErrorInfo>();
        Assert.Equal("operations", info.Domain); Assert.Equal(code, info.Reason);
    }
}
