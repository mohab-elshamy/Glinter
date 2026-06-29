using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayBookingPlatformConfiguration : IEntityTypeConfiguration<StayBookingPlatform>
{
    public void Configure(EntityTypeBuilder<StayBookingPlatform> builder)
    {
        builder.ToTable("stay_booking_platforms");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.PriceWithTax)
            .HasPrecision(18, 2);

        builder.Property(x => x.Link)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.StayId);
        builder.HasIndex(x => new { x.StayId, x.Name, x.Link }).IsUnique();
    }
}
