using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayAmenityConfiguration : IEntityTypeConfiguration<StayAmenity>
{
    public void Configure(EntityTypeBuilder<StayAmenity> builder)
    {
        builder.ToTable("stay_amenities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr)
            .HasMaxLength(250);

        builder.Property(x => x.NameEn)
            .HasMaxLength(250);

        builder.HasIndex(x => x.StayId);
        builder.HasIndex(x => new { x.StayId, x.NameAr, x.NameEn }).IsUnique();
    }
}
