using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public sealed class BuddyAvailabilityConfiguration : IEntityTypeConfiguration<BuddyAvailability>
{
    public void Configure(EntityTypeBuilder<BuddyAvailability> builder)
    {
        builder.ToTable("buddy_availability");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.HasIndex(x => new { x.LocalBuddyUserId, x.StartTimeUtc, x.EndTimeUtc });
        builder.HasMany(x => x.Bookings)
            .WithOne(x => x.Availability)
            .HasForeignKey(x => x.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
