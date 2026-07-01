using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceAvailabilityConfiguration : IEntityTypeConfiguration<ExperienceAvailability>
{
    public void Configure(EntityTypeBuilder<ExperienceAvailability> builder)
    {
        builder.ToTable("experience_availability");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PricePerPerson).HasPrecision(18, 2);
        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.StartTimeUtc, x.EndTimeUtc });
        builder.HasMany(x => x.Bookings)
            .WithOne(x => x.Availability)
            .HasForeignKey(x => x.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
