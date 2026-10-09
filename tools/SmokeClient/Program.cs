using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using MPCore.Audit;
using Npgsql;
using FleetCompany.FleetManagement.Api.Grpc.Fleet;
using FleetCompany.FleetManagement.Api.Grpc.Operations;

var identityUri = Environment.GetEnvironmentVariable("FLEET_SMOKE_IDENTITY_URI") ?? "http://localhost:55480";
var restUri = Environment.GetEnvironmentVariable("FLEET_SMOKE_REST_URI") ?? "http://localhost:8080";
var grpcUri = Environment.GetEnvironmentVariable("FLEET_SMOKE_GRPC_URI") ?? "http://localhost:8081";
using var identity = new HttpClient
{
    BaseAddress = new Uri(identityUri)
};
using var http = new HttpClient
{
    BaseAddress = new Uri(restUri),
    Timeout = TimeSpan.FromSeconds(45)
};
async Task<string> Token(string role)
{
    using var response = await identity.PostAsJsonAsync("/development/token", new { role });
    response.EnsureSuccessStatusCode();
    var value = await response.Content.ReadFromJsonAsync<JsonElement>();
    return value.GetProperty("access_token").GetString()!;
}

var manager = await Token("FleetManager");
var operatorToken = await Token("Operator");
var administrator = await Token("Administrator");
void Use(string token) => http.DefaultRequestHeaders.Authorization = new("Bearer", token);
async Task<JsonElement> Post(string path, object request)
{
    using var response = await http.PostAsJsonAsync(path, request);
    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException($"{path}: HTTP {(int)response.StatusCode}; {await response.Content.ReadAsStringAsync()}");
    return await response.Content.ReadFromJsonAsync<JsonElement>();
}

async Task<JsonElement> Get(string path)
{
    using var response = await http.GetAsync(path);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<JsonElement>();
}

void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
    Console.WriteLine("PASS: " + message);
}

Check((await http.GetAsync("/health/ready")).IsSuccessStatusCode, "Application readiness");
using (var wrongListener = new HttpRequestMessage(HttpMethod.Get, grpcUri + "/api/fleet/vehicles/available")
{
    Version = HttpVersion.Version20,
    VersionPolicy = HttpVersionPolicy.RequestVersionExact
}

)
using (var separated = await http.SendAsync(wrongListener))
    Check(separated.StatusCode == HttpStatusCode.NotFound, "REST is not exposed on the gRPC listener");
var runtimeConnection = Environment.GetEnvironmentVariable("FLEET_CONTAINER_POSTGRES_CONNECTION");
if (!string.IsNullOrWhiteSpace(runtimeConnection))
{
    var postgresPort = int.Parse(Environment.GetEnvironmentVariable("FLEET_SMOKE_POSTGRES_PORT") ?? "55432", System.Globalization.CultureInfo.InvariantCulture);
    var localConnection = new NpgsqlConnectionStringBuilder(runtimeConnection)
    {
        Host = "localhost",
        Port = postgresPort
    };
    await using var db = new NpgsqlConnection(localConnection.ConnectionString);
    await db.OpenAsync();
    await using var permissions = new NpgsqlCommand("SELECT has_table_privilege(current_user,'audit.entries','SELECT'), has_table_privilege(current_user,'audit.entries','INSERT'), has_table_privilege(current_user,'audit.entries','UPDATE'), has_table_privilege(current_user,'audit.entries','DELETE'), has_table_privilege(current_user,'audit.entries','TRUNCATE')", db);
    await using var row = await permissions.ExecuteReaderAsync();
    await row.ReadAsync();
    Check(row.GetBoolean(0) && row.GetBoolean(1) && !row.GetBoolean(2) && !row.GetBoolean(3) && !row.GetBoolean(4), "Runtime Audit permissions are SELECT/INSERT only");
}

using (var anonymous = await http.GetAsync("/api/fleet/vehicles/available"))
    Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "Anonymous REST is rejected");
Use(manager);
var vehicle = await Post("/api/fleet/vehicles/", new { plateNumber = "SMOKE-" + Guid.NewGuid().ToString("N")[..20], typeCode = "TRUCK", capacityKilograms = 2000, baseStatus = "Active" });
var vehicleId = vehicle.GetProperty("id").GetString()!;
var driver = await Post("/api/drivers/", new { firstName = "Local", lastName = "Smoke", status = 1, qualifications = new[] { "TRUCK" } });
Check(vehicle.GetProperty("baseStatus").GetString() == "Active" && vehicle.GetProperty("baseStatusName").GetString() == "Active", "REST exposes named vehicle status");
Check(driver.GetProperty("status").GetString() == "Active" && driver.GetProperty("statusName").GetString() == "Active", "REST exposes named driver status");
Use(operatorToken);
var restoredDriver = await Get($"/api/drivers/{driver.GetProperty("id").GetString()}");
Check(restoredDriver.GetProperty("id").GetGuid() == driver.GetProperty("id").GetGuid(), "Live GetDriver reads the created driver");
var mission = await Post("/api/operations/missions/", new { origin = "Local A", destination = "Local B", requiredCapacityKilograms = 1000 });
var missionId = mission.GetProperty("id").GetString()!;
await Post($"/api/operations/missions/{missionId}/schedule", new { scheduledTime = DateTimeOffset.UtcNow.AddHours(1).ToString("O") });
await Post($"/api/operations/missions/{missionId}/assign", new { vehicleId, driverId = driver.GetProperty("id").GetString() });
Check(!(await Get("/api/fleet/vehicles/available?limit=200")).EnumerateArray().Any(x => x.GetProperty("id").GetString() == vehicleId), "Assigned vehicle disappears from availability");
using var channel = GrpcChannel.ForAddress(grpcUri);
var headers = new Metadata
{
    {
        "authorization",
        "Bearer " + operatorToken
    }
};
var fleet = new FleetVehicles.FleetVehiclesClient(channel);
var operations = new OperationsMissions.OperationsMissionsClient(channel);
var deadline = DateTime.UtcNow.AddSeconds(30);
var grpcVehicle = await fleet.GetVehicleAsync(new GetVehicleRequest { Id = vehicleId }, headers, deadline);
Check(grpcVehicle.Vehicle.Id == vehicleId, "Live gRPC GetVehicle with OIDC discovery");
var grpcAvailable = await fleet.GetAvailableVehiclesAsync(new GetAvailableVehiclesRequest { Limit = 200 }, headers, deadline);
Check(grpcAvailable.Vehicles.All(x => x.Id != vehicleId), "Live gRPC GetAvailableVehicles");
var grpcMission = await operations.GetMissionAsync(new GetMissionRequest { Id = missionId }, headers, deadline);
Check(grpcMission.Mission.Status == MissionState.Assigned, "Live gRPC GetMission");
var active = await operations.GetActiveMissionsAsync(new GetActiveMissionsRequest { Limit = 200 }, headers, deadline);
Check(active.Missions.Any(x => x.Id == missionId), "Live gRPC GetActiveMissions");
try
{
    await operations.GetMissionAsync(new GetMissionRequest { Id = missionId }, deadline: deadline);
    throw new InvalidOperationException("Anonymous gRPC unexpectedly succeeded.");
}
catch (RpcException error)when (error.StatusCode == StatusCode.Unauthenticated)
{
    Console.WriteLine("PASS: Anonymous gRPC is rejected");
}

Use(manager);
using (var rejected = await http.PostAsJsonAsync($"/api/fleet/vehicles/{vehicleId}/maintenance/start", new { }))
    Check(rejected.StatusCode == HttpStatusCode.Conflict, "Maintenance rejects an assigned vehicle");
Use(operatorToken);
await Post($"/api/operations/missions/{missionId}/start", new { });
await Post($"/api/operations/missions/{missionId}/complete", new { });
Use(administrator);
var audit = await Get($"/api/administration/audit/?entityId={missionId}&size=200");
foreach (var action in new[]
{
    "MissionCreated",
    "MissionAssigned",
    "MissionStarted",
    "MissionCompleted"
}

)
    Check(audit.GetProperty("items").EnumerateArray().Any(x => x.GetProperty("action").GetString() == action), "Audit " + action);
var rejectedAudit = await Get($"/api/administration/audit/?entityId={vehicleId}&size=200");
Check(rejectedAudit.GetProperty("items").EnumerateArray().Any(x => x.GetProperty("action").GetString() == "MaintenanceStarted" && x.GetProperty("outcome").GetInt32() == (int)AuditOutcome.Rejected), "Rejected operation remains auditable");
Use(manager);
using var inactive = await http.PutAsJsonAsync($"/api/fleet/vehicles/{vehicleId}/status", new { status = 2 });
inactive.EnsureSuccessStatusCode();
var updatedVehicle = await inactive.Content.ReadFromJsonAsync<JsonElement>();
Check(updatedVehicle.GetProperty("baseStatus").GetString() == "Inactive" && updatedVehicle.GetProperty("baseStatusName").GetString() == "Inactive", "Status mutation returns the updated vehicle representation");
Console.WriteLine("Local demo mission completed; its vehicle is now inactive. Fictional records and audits remain for review.");
