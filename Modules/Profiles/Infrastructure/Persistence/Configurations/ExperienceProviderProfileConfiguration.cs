using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public class ExperienceProviderProfileConfiguration : IEntityTypeConfiguration<ExperienceProviderProfile>
{
    public void Configure(EntityTypeBuilder<ExperienceProviderProfile> builder)
    {
        builder.ToTable("experience_provider_profiles");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.BusinessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ContactPersonName)
            .HasMaxLength(200);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);
        
        builder.Property(x => x.ProfileImageUrl)
            .HasMaxLength(1000);
    }
}