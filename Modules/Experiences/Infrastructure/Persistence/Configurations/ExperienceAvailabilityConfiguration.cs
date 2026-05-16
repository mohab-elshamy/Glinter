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

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => x.StartTimeUtc);
        builder.HasIndex(x => x.IsActive);

        builder.Property(x => x.StartTimeUtc)
            .IsRequired();

        builder.Property(x => x.EndTimeUtc)
            .IsRequired();

        builder.Property(x => x.Capacity)
            .IsRequired();

        builder.Property(x => x.BookedCount)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Experience)
            .WithMany(x => x.AvailabilitySlots)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}