using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FleetCompany.FleetManagement.Modules.Operations.Domain.Missions;

namespace FleetCompany.FleetManagement.Modules.Operations.Infrastructure.Persistence;

public sealed class MissionConfiguration : IEntityTypeConfiguration<Mission>
{
    public const string ActiveVehicleUniqueIndex = "ux_active_mission_vehicle";
    public const string ActiveDriverUniqueIndex = "ux_active_mission_driver";
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder.ToTable("missions", "operations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Origin).HasConversion(x => x.Value, x => MissionLocation.Create(x)).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Destination).HasConversion(x => x.Value, x => MissionLocation.Create(x)).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequiredCapacity).HasConversion(x => x.Kilograms, x => RequiredCapacity.Create(x)).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ScheduledTime).HasColumnType("timestamp with time zone");
        builder.Property(x => x.AssignedVehicleId);
        builder.Property(x => x.AssignedDriverId);
        builder.HasIndex(x => x.AssignedVehicleId).IsUnique().HasDatabaseName(ActiveVehicleUniqueIndex).HasFilter("\"Status\" IN (3,4)");
        builder.HasIndex(x => x.AssignedDriverId).IsUnique().HasDatabaseName(ActiveDriverUniqueIndex).HasFilter("\"Status\" IN (3,4)");
        builder.Ignore(x => x.HasActiveReservation);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IntegrationEvents);
    }
}
