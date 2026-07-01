using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public sealed class StayAmenityConfiguration
    : IEntityTypeConfiguration<StayAmenity>
{
    public void Configure(
        EntityTypeBuilder<StayAmenity> builder)
    {
        builder.ToTable(
            "stay_amenities",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_stay_amenities_has_name",
                    """
                    NULLIF(BTRIM("NameAr"), '') IS NOT NULL
                    OR NULLIF(BTRIM("NameEn"), '') IS NOT NULL
                    """);
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameAr)
            .HasMaxLength(250);

        builder.Property(x => x.NameEn)
            .HasMaxLength(250);

        builder.HasOne(x => x.Stay)
            .WithMany(x => x.Amenities)
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.StayId);

        builder.HasIndex(
                x => new
                {
                    x.StayId,
                    x.NameAr,
                    x.NameEn
                })
            .IsUnique()
            .AreNullsDistinct(false);
    }
}