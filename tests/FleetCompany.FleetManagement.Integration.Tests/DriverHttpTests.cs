using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FleetCompany.FleetManagement.Infrastructure.Persistence;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
using Xunit;
namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class DriverHttpTests
{
    [HttpDependenciesFact]
    public async Task Registration_checks_authorization_validation_and_persisted_qualifications()
    {
        await using var factory = new FleetTestHost();
        using var http = factory.CreateClient();
        var name = "DriverTest-" + Guid.NewGuid().ToString("N");
        object Request(DriverStatus? status = DriverStatus.Active, string?[]? codes = null)
            => new { firstName = name, lastName = " موزفری ", status, qualifications = codes ?? [" truck ", "TRUCK", "BUS"] };
        try
        {
            using var anonymous = await http.PostAsJsonAsync("/api/drivers/", Request());
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("Operator"));
            using var denied = await http.PostAsJsonAsync("/api/drivers/", Request());
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token("FleetManager"));
            foreach (var invalid in new[] { Request(null), Request(codes: []), Request(codes: ["BUS", "BAD-CODE"]) })
            {
                using var response = await http.PostAsJsonAsync("/api/drivers/", invalid);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.Equal(0, await db.Set<Driver>().CountAsync(x => x.FirstName == DriverName.Create(name)));
            }
            foreach (var status in new[] { DriverStatus.Active, DriverStatus.Inactive })
            {
                using var response = await http.PostAsJsonAsync("/api/drivers/", Request(status));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var id = body.RootElement.GetProperty("id").GetGuid();
                Assert.Equal(2, body.RootElement.GetProperty("qualifications").GetArrayLength());
                using var scope = factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var saved = await db.Set<Driver>().AsNoTracking().SingleAsync(x => x.Id == id);
                Assert.Equal(name, saved.FirstName.Value);
                Assert.Equal("موزفری", saved.LastName.Value);
                Assert.Equal(status, saved.Status);
                Assert.Equal(status == DriverStatus.Active, saved.IsActive);
                Assert.Equal(2, saved.Qualifications.Count);
                Assert.True(saved.IsQualifiedFor(QualificationCode.Create("BUS")));
                Assert.False(saved.IsQualifiedFor(QualificationCode.Create("VAN")));
            }
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Set<Driver>().Where(x => x.FirstName == DriverName.Create(name)).ExecuteDeleteAsync();
        }
    }
}
