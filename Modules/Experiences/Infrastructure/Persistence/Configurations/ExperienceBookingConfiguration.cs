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
        builder.Property(x => x.TravelerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TotalPrice).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => x.AvailabilityId);
        builder.HasIndex(x => x.TravelerProfileId);
        builder.HasIndex(x => x.CreatedByUserId);
    }
}
