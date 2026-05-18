using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence;

public sealed class DistrictIndexConfiguration : IEntityTypeConfiguration<DistrictIndex>
{
    public void Configure(EntityTypeBuilder<DistrictIndex> builder)
    {
        builder.ToTable("district_indices");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.DistrictId)
            .IsUnique();

        builder.Property(x => x.SafetyScore)
            .IsRequired();

        builder.Property(x => x.SafetyLevel)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SafetyExplanation)
            .HasMaxLength(1000);

        builder.Property(x => x.PriceLevel)
            .HasMaxLength(50);

        builder.Property(x => x.PriceExplanation)
            .HasMaxLength(1000);

        builder.Property(x => x.ServicesLevel)
            .HasMaxLength(50);

        builder.Property(x => x.ServicesExplanation)
            .HasMaxLength(1000);

        builder.Property(x => x.ComputedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.District)
            .WithOne()
            .HasForeignKey<DistrictIndex>(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}