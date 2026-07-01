using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public sealed class BuddyBookingConfiguration : IEntityTypeConfiguration<BuddyBooking>
{
    public void Configure(EntityTypeBuilder<BuddyBooking> builder)
    {
        builder.ToTable("buddy_bookings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalPrice).HasPrecision(12, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.LocalBuddyUserId, x.Status });
        builder.HasIndex(x => new { x.TravelerUserId, x.Status });
        builder.HasIndex(x => x.AvailabilityId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Accepted')");
    }
}
