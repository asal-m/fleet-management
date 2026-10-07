using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;
using Xunit;
namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class MissionHttpTests
{
    [HttpDependenciesFact]
    public async Task Create_mission_validates_authorizes_and_persists_only_a_draft()
    {
        await using var factory = new FleetTestHost();
        using var http = factory.CreateClient();
        var origin = "MissionTest-" + Guid.NewGuid().ToString("N");
        object Request(string? destination = " شیراز ", int capacity = 1000) => new
        {
            origin = " " + origin + " ",
            destination,
            requiredCapacityKilograms = capacity,
            status = 4,
            scheduledTime = DateTimeOffset.UtcNow,
            assignedVehicleId = Guid.NewGuid(),
            assignedDriverId = Guid.NewGuid()
        };
        try
        {
            using var anonymous = await http.PostAsJsonAsync("/api/operations/missions/", Request());
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            foreach (var role in new[] { "FleetManager", "Administrator" })
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(role));
                using var denied = await http.PostAsJsonAsync("/api/operations/missions/", Request());
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            }
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Operator"));
            foreach (var invalid in new[] { Request(null), Request("  "), Request(capacity: 0), Request(capacity: -1), Request(new string('A', 501)) })
            {
                using var response = await http.PostAsJsonAsync("/api/operations/missions/", invalid);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(0, await db.Set<Mission>().CountAsync(x => x.Origin == MissionLocation.Create(origin)));
            }
            foreach (var destination in new[] { " شیراز ", origin })
            {
                using var response = await http.PostAsJsonAsync("/api/operations/missions/", Request(destination));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var id = body.RootElement.GetProperty("id").GetGuid();
                Assert.Equal($"/api/operations/missions/{id}", response.Headers.Location!.OriginalString);
                using var fetched = await http.GetAsync(response.Headers.Location);
                Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
                using var fetchedBody = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync());
                Assert.Equal(body.RootElement.GetRawText(), fetchedBody.RootElement.GetRawText());
                Assert.Equal(1, body.RootElement.GetProperty("status").GetInt32());
                Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("scheduledTime").ValueKind);
                Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("assignedVehicleId").ValueKind);
                Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("assignedDriverId").ValueKind);
                using var scope = factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var saved = await db.Set<Mission>().AsNoTracking().SingleAsync(x => x.Id == id);
                Assert.Equal(origin, saved.Origin.Value);
                Assert.Equal(destination.Trim(), saved.Destination.Value);
                Assert.Equal(1000, saved.RequiredCapacity.Kilograms);
                Assert.Equal(MissionStatus.Draft, saved.Status);
                Assert.Null(saved.ScheduledTime);
                Assert.Null(saved.AssignedVehicleId);
                Assert.Null(saved.AssignedDriverId);
                Assert.False(saved.HasActiveReservation);
            }
            using var missing = await http.GetAsync($"/api/operations/missions/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Contains("MISSION_NOT_FOUND", await missing.Content.ReadAsStringAsync());
            http.DefaultRequestHeaders.Authorization = null;
            using var missingToken = await http.GetAsync($"/api/operations/missions/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.Unauthorized, missingToken.StatusCode);
            foreach (var role in new[] { "FleetManager", "Administrator" })
            {
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(role));
                using var deniedRead = await http.GetAsync($"/api/operations/missions/{Guid.NewGuid()}");
                Assert.Equal(HttpStatusCode.Forbidden, deniedRead.StatusCode);
            }
            using (var countScope = factory.Services.CreateScope())
            {
                var countDatabase = countScope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(2, await countDatabase.Set<Mission>().CountAsync(x => x.Origin == MissionLocation.Create(origin)));
            }
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Set<Mission>().Where(x => x.Origin == MissionLocation.Create(origin)).ExecuteDeleteAsync();
        }
    }
}
