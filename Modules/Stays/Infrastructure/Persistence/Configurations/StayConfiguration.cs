using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayConfiguration : IEntityTypeConfiguration<Stay>
{
    public void Configure(EntityTypeBuilder<Stay> builder)
    {
        builder.ToTable("stays");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2);

        builder.Property(x => x.Description)
            .HasMaxLength(5000);

        builder.Property(x => x.GoogleMapsLink)
            .HasMaxLength(2000);

        builder.Property(x => x.Website)
            .HasMaxLength(2000);

        builder.Property(x => x.PhoneInternational)
            .HasMaxLength(50);

        builder.Property(x => x.LocationSummaryDescription)
            .HasMaxLength(2000);

        builder.Property(x => x.Cid)
            .HasMaxLength(100);

        builder.Property(x => x.Rating)
            .HasPrecision(3, 2);

        builder.HasIndex(x => x.Cid).IsUnique();
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.Adm0Gid);
        builder.HasIndex(x => x.Adm1Gid);
        builder.HasIndex(x => x.Adm2Gid);
        builder.HasIndex(x => x.Adm3Gid);
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => x.Rating);
        builder.HasIndex(x => new { x.Latitude, x.Longitude });
        builder.HasIndex(x => x.HotelOwnerProfileId);

        builder.HasMany(x => x.Images)
            .WithOne(x => x.Stay)
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Amenities)
            .WithOne(x => x.Stay)
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReviewsPerRatings)
            .WithOne(x => x.Stay)
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BookingPlatforms)
            .WithOne(x => x.Stay)
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StayReviews)
            .WithOne(x => x.Stay)
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
