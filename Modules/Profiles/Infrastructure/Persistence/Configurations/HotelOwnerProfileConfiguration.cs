using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public class HotelOwnerProfileConfiguration : IEntityTypeConfiguration<HotelOwnerProfile>
{
    public void Configure(EntityTypeBuilder<HotelOwnerProfile> builder)
    {
        builder.ToTable("hotel_owner_profiles");

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