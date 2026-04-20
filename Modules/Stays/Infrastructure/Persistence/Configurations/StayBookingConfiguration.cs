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

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TotalPrice)
            .HasColumnType("numeric(18,2)");
    }
}