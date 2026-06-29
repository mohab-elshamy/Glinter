using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperiencePopularTimeConfiguration : IEntityTypeConfiguration<ExperiencePopularTime>
{
    public void Configure(EntityTypeBuilder<ExperiencePopularTime> builder)
    {
        builder.ToTable("experience_popular_times");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.DayOfWeek, x.HourOfDay }).IsUnique();
    }
}
