using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceTagConfiguration : IEntityTypeConfiguration<ExperienceTag>
{
    public void Configure(EntityTypeBuilder<ExperienceTag> builder)
    {
        builder.ToTable("experience_tags");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ExperienceId, x.Name })
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Experience)
            .WithMany(x => x.Tags)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}