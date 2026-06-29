using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("experiences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.SourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Description)
            .HasMaxLength(4000);

        builder.Property(x => x.Address)
            .HasMaxLength(750);

        builder.Property(x => x.Cid)
            .HasMaxLength(100);

        builder.Property(x => x.Adm0Gid);
        builder.Property(x => x.Adm1Gid);
        builder.Property(x => x.Adm2Gid);
        builder.Property(x => x.Adm3Gid);

        builder.Property(x => x.GoogleMapsLink)
            .HasMaxLength(2000);

        builder.Property(x => x.PhoneInternational)
            .HasMaxLength(50);

        builder.Property(x => x.PriceRange)
            .HasMaxLength(100);

        builder.Property(x => x.Website)
            .HasMaxLength(2000);

        builder.Property(x => x.Rating)
            .HasPrecision(3, 2);

        builder.HasIndex(x => x.Cid)
            .IsUnique();

        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.Adm0Gid);
        builder.HasIndex(x => x.Adm1Gid);
        builder.HasIndex(x => x.Adm2Gid);
        builder.HasIndex(x => x.Adm3Gid);
        builder.HasIndex(x => new { x.Latitude, x.Longitude });
        builder.HasIndex(x => x.Rating);
        builder.HasIndex(x => x.ProviderProfileId);

        builder.HasMany(x => x.FeaturedImages)
            .WithOne(x => x.Experience)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Hours)
            .WithOne(x => x.Experience)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PopularTimes)
            .WithOne(x => x.Experience)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReviewsPerRatings)
            .WithOne(x => x.Experience)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Amenities)
            .WithOne(x => x.Experience)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ExperienceReviews)
            .WithOne(x => x.Experience)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
