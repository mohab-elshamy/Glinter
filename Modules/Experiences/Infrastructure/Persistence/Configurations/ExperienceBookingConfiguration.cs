using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceBookingConfiguration : IEntityTypeConfiguration<ExperienceBooking>
{
    public void Configure(EntityTypeBuilder<ExperienceBooking> builder)
    {
        builder.ToTable("experience_bookings");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => x.AvailabilityId);
        builder.HasIndex(x => x.TravelerProfileId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.ExperienceId, x.Status })
            .IsCreatedConcurrently();
        builder.HasIndex(x => new { x.CreatedAtUtc, x.Status })
            .IsCreatedConcurrently();
        builder.HasIndex(x => new
        {
            x.AvailabilityId,
            x.TravelerProfileId
        })
            .IsUnique()
            .HasFilter("\"Status\" <> 'Cancelled'");

        builder.Property(x => x.GuestsCount)
            .IsRequired();

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Experience)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Availability)
            .WithMany(x => x.Bookings)
            .HasForeignKey(x => x.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
