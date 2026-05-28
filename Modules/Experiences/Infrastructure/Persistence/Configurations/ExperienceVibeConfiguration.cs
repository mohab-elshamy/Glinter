using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceVibeConfiguration : IEntityTypeConfiguration<ExperienceVibe>
{
    public void Configure(EntityTypeBuilder<ExperienceVibe> builder)
    {
        builder.ToTable("experience_vibes");

        builder.HasKey(x => new { x.ExperienceId, x.VibeId });

        builder.HasOne(x => x.Experience)
            .WithMany(x => x.ExperienceVibes)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Vibe)
            .WithMany(x => x.ExperienceVibes)
            .HasForeignKey(x => x.VibeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}