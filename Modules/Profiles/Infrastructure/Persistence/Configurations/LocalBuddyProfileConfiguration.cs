using Glinter.Modules.Profiles.Domain.Entities;
using Glinter.Modules.Profiles.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public class LocalBuddyProfileConfiguration : IEntityTypeConfiguration<LocalBuddyProfile>
{
    public void Configure(EntityTypeBuilder<LocalBuddyProfile> builder)
    {
        builder.ToTable("local_buddy_profiles");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Bio)
            .HasMaxLength(1000);

        builder.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Languages)
            .HasMaxLength(500);

        builder.Property(x => x.Rating)
            .HasPrecision(3, 2);

        builder.Property(x => x.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(VerificationStatus.Pending);
        
        builder.Property(x => x.ProfileImageUrl)
            .HasMaxLength(1000);
    }
}