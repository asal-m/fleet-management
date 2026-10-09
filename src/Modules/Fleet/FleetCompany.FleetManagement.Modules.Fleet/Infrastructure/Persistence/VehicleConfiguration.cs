using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FleetCompany.FleetManagement.Modules.Fleet.Domain.Vehicles;

namespace FleetCompany.FleetManagement.Modules.Fleet.Infrastructure.Persistence;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public const string PlateUniqueIndex = "ux_vehicles_plate_number";
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", "fleet");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).ValueGeneratedNever();
        builder.Property(vehicle => vehicle.PlateNumber).HasConversion(value => value.Value, value => PlateNumber.Create(value)).HasMaxLength(30).IsRequired();
        builder.HasIndex(vehicle => vehicle.PlateNumber).IsUnique().HasDatabaseName(PlateUniqueIndex);
        builder.Property(vehicle => vehicle.TypeCode).HasConversion(value => value.Value, value => VehicleTypeCode.Create(value)).HasMaxLength(50).IsRequired();
        builder.Property(vehicle => vehicle.Capacity).HasConversion(value => value.Kilograms, value => VehicleCapacity.Create(value)).IsRequired();
        builder.Property(vehicle => vehicle.BaseStatus).IsRequired();
        builder.Property(vehicle => vehicle.IsUnderMaintenance).IsRequired();
        builder.Ignore(vehicle => vehicle.OperationalStatus);
        builder.Ignore(vehicle => vehicle.IsOperationalForAssignment);
        builder.Ignore(vehicle => vehicle.DomainEvents);
        builder.Ignore(vehicle => vehicle.IntegrationEvents);
    }
}
