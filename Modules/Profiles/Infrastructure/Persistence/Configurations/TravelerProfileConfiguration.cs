using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public class TravelerProfileConfiguration : IEntityTypeConfiguration<TravelerProfile>
{
    public void Configure(EntityTypeBuilder<TravelerProfile> builder)
    {
        builder.ToTable("traveler_profiles");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Bio)
            .HasMaxLength(1000);

        builder.Property(x => x.Nationality)
            .HasMaxLength(100);

        builder.Property(x => x.PreferredBudgetLevel)
            .HasMaxLength(50);

        builder.Property(x => x.TravelStyle)
            .HasMaxLength(100);

        builder.Property(x => x.PreferredInterests)
            .HasMaxLength(1000);

        builder.Property(x => x.ProfileImageUrl)
            .HasMaxLength(1000);
    }
}