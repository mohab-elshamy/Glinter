using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceHourConfiguration : IEntityTypeConfiguration<ExperienceHour>
{
    public void Configure(EntityTypeBuilder<ExperienceHour> builder)
    {
        builder.ToTable("experience_hours");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.DayOfWeek, x.OpensAt, x.ClosesAt }).IsUnique();
    }
}
