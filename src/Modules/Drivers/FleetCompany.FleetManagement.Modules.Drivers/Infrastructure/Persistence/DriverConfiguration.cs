using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FleetCompany.FleetManagement.Modules.Drivers.Domain.Drivers;
namespace FleetCompany.FleetManagement.Modules.Drivers.Infrastructure.Persistence;

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers", "drivers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FirstName).HasConversion(x => x.Value, x => DriverName.Create(x)).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasConversion(x => x.Value, x => DriverName.Create(x)).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Ignore(x => x.Qualifications);
        var qualifications = builder.Property<List<QualificationCode>>("_qualifications")
            .HasColumnName("Qualifications").HasColumnType("jsonb").IsRequired()
            .HasConversion(x => Encode(x), x => Decode(x));
        qualifications.Metadata.SetValueComparer(new ValueComparer<List<QualificationCode>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            x => x.Aggregate(0, (hash, code) => HashCode.Combine(hash, code.GetHashCode())),
            x => x.ToList()));
        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IntegrationEvents);
    }
    private static string Encode(List<QualificationCode> codes) => JsonSerializer.Serialize(codes.Select(x => x.Value).ToArray());
    private static List<QualificationCode> Decode(string json)
        => JsonSerializer.Deserialize<string[]>(json)!.Select(QualificationCode.Create).ToList();
}
