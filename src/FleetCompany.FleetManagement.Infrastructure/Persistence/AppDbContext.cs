using Microsoft.EntityFrameworkCore;
using MPCore.Audit.EntityFrameworkCore;
using MPCore.Domain.Events;
using MPCore.Persistence.EntityFrameworkCore.PostgreSql;

namespace FleetCompany.FleetManagement.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider, IAggregateEventSink eventSink) : MPCoreDbContext(options, timeProvider, eventSink)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(FleetCompany.FleetManagement.Modules.Fleet.AssemblyReference.Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(FleetCompany.FleetManagement.Modules.Drivers.AssemblyReference.Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(FleetCompany.FleetManagement.Modules.Operations.AssemblyReference.Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(FleetCompany.FleetManagement.Modules.Administration.AssemblyReference.Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ApplyMPCoreAudit();
        base.OnModelCreating(modelBuilder);
    }
}
