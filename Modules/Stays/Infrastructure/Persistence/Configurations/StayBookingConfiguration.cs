using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayBookingConfiguration : IEntityTypeConfiguration<StayBooking>
{
    public void Configure(EntityTypeBuilder<StayBooking> builder)
    {
        builder.ToTable("stay_bookings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GuestName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(x => x.StayId);
        builder.HasIndex(x => x.TravelerProfileId);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => new { x.StayId, x.CheckInDate, x.CheckOutDate });
    }
}
