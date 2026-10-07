using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class HttpDependenciesFactAttribute : FactAttribute
{
    public HttpDependenciesFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION"))
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FLEET_TEST_REDIS_CONNECTION")))
            Skip = "Run scripts/Start-LocalDependencies.ps1 -RunTests with both PostgreSQL and Redis.";
    }
}

public sealed class VehicleHttpTests
{
    [HttpDependenciesFact]
    public async Task Registration_enforces_authentication_authorization_validation_and_transaction()
    {
        await using var factory = new FleetTestHost();
        using var client = factory.CreateClient();
        var plate = "HTTP-" + Guid.NewGuid().ToString("N")[..20];
        var request = new { plateNumber = plate, typeCode = " truck ", capacityKilograms = 1000, baseStatus = 1 };
        try
        {
            using var anonymous = await client.PostAsJsonAsync("/api/fleet/vehicles/", request);
            await AssertProblem(anonymous, HttpStatusCode.Unauthorized);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Operator"));
            using var forbidden = await client.PostAsJsonAsync("/api/fleet/vehicles/", request);
            await AssertProblem(forbidden, HttpStatusCode.Forbidden);
            await AssertCount(factory, plate, 0);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("FleetManager", wrongAudience: true));
            using var wrongAudience = await client.PostAsJsonAsync("/api/fleet/vehicles/", request);
            await AssertProblem(wrongAudience, HttpStatusCode.Unauthorized);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("FleetManager", wrongSignature: true));
            using var wrongSignature = await client.PostAsJsonAsync("/api/fleet/vehicles/", request);
            await AssertProblem(wrongSignature, HttpStatusCode.Unauthorized);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("FleetManager"));
            using var invalid = await client.PostAsJsonAsync("/api/fleet/vehicles/", new
            {
                plateNumber = plate,
                typeCode = "TRUCK",
                capacityKilograms = 0
            });
            await AssertProblem(invalid, HttpStatusCode.BadRequest);
            await AssertCount(factory, plate, 0);

            using var created = await client.PostAsJsonAsync("/api/fleet/vehicles/", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var representation = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            Assert.NotEqual(Guid.Empty, representation.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("TRUCK", representation.RootElement.GetProperty("typeCode").GetString());
            Assert.False(representation.RootElement.GetProperty("isUnderMaintenance").GetBoolean());
            await AssertCount(factory, plate, 1);

            Assert.NotNull(created.Headers.Location);
            var location = created.Headers.Location!.ToString();
            using var fetched = await client.GetAsync(location);
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
            using var fetchedBody = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync());
            Assert.Equal(representation.RootElement.GetProperty("id").GetGuid(), fetchedBody.RootElement.GetProperty("id").GetGuid());
            Assert.Equal(plate.ToUpperInvariant(), fetchedBody.RootElement.GetProperty("plateNumber").GetString());

            client.DefaultRequestHeaders.Authorization = null;
            using var anonymousRead = await client.GetAsync(location);
            await AssertProblem(anonymousRead, HttpStatusCode.Unauthorized);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Administrator"));
            using var forbiddenRead = await client.GetAsync(location);
            await AssertProblem(forbiddenRead, HttpStatusCode.Forbidden);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Operator"));
            using var operatorRead = await client.GetAsync(location);
            Assert.Equal(HttpStatusCode.OK, operatorRead.StatusCode);
            using var missing = await client.GetAsync($"/api/fleet/vehicles/{Guid.NewGuid()}");
            await AssertProblem(missing, HttpStatusCode.NotFound);
            Assert.Contains("VEHICLE_NOT_FOUND", await missing.Content.ReadAsStringAsync());
            await AssertCount(factory, plate, 1);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("FleetManager"));

            using var duplicate = await client.PostAsJsonAsync("/api/fleet/vehicles/", request);
            await AssertProblem(duplicate, HttpStatusCode.Conflict);
            Assert.Contains("PLATE_NUMBER_ALREADY_REGISTERED", await duplicate.Content.ReadAsStringAsync());
            await AssertCount(factory, plate, 1);
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var normalizedPlate = PlateNumber.Create(plate);
            await database.Set<Vehicle>().Where(vehicle => vehicle.PlateNumber == normalizedPlate).ExecuteDeleteAsync();
        }
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
    }

    private static async Task AssertCount(FleetTestHost factory, string plate, int expected)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedPlate = PlateNumber.Create(plate);
        Assert.Equal(expected, await database.Set<Vehicle>().CountAsync(vehicle => vehicle.PlateNumber == normalizedPlate));
    }
}

public sealed class FleetTestHost : WebApplicationFactory<Program>
{
    private const string TestIssuer = "https://fleet-test-issuer.example.test";
    private const string TestAudience = "fleet-http-tests";
    private readonly RSA signingKey = RSA.Create(2048);
    private readonly RSA unrelatedKey = RSA.Create(2048);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Match the Linux container's console logging; the Windows EventLog provider
        // has an OS handle lifetime unrelated to this in-memory test host.
        builder.ConfigureLogging(logging => logging.ClearProviders().AddJsonConsole(options => options.IncludeScopes = true));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Security:Authority"] = TestIssuer,
                ["Security:Audiences:0"] = TestAudience,
                ["ConnectionStrings:PostgreSql"] = Environment.GetEnvironmentVariable("FLEET_TEST_POSTGRES_CONNECTION"),
                ["ConnectionStrings:Redis"] = Environment.GetEnvironmentVariable("FLEET_TEST_REDIS_CONNECTION"),
                ["Transport:EnableOpenApi"] = "false",
                ["Transport:EnableGrpcReflection"] = "false"
            }));
        builder.ConfigureTestServices(services => services.PostConfigureAll<JwtBearerOptions>(options =>
        {
            // Test metadata replaces network discovery only. The production JWT signature,
            // issuer, audience, expiry and role validation are not replaced or disabled.
            var metadata = new OpenIdConnectConfiguration { Issuer = TestIssuer };
            var publicKey = new RsaSecurityKey(signingKey.ExportParameters(false)) { KeyId = "fleet-test-key" };
            metadata.SigningKeys.Add(publicKey);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        }));
    }

    public string Token(string role, bool wrongAudience = false, bool wrongSignature = false)
    {
        var now = DateTime.UtcNow;
        var key = new RsaSecurityKey(wrongSignature ? unrelatedKey : signingKey) { KeyId = "fleet-test-key" };
        var payload = new JwtPayload(TestIssuer, wrongAudience ? "other-api" : TestAudience, null,
            now.AddMinutes(-1), now.AddMinutes(5), now);
        payload["sub"] = "fleet-http-test-user";
        payload["preferred_username"] = "fleet-http-test-user";
        payload["realm_access"] = new Dictionary<string, object> { ["roles"] = new[] { role } };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.RsaSha256)), payload));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { signingKey.Dispose(); unrelatedKey.Dispose(); }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        signingKey.Dispose();
        unrelatedKey.Dispose();
    }
}
