using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceFeaturedImageConfiguration : IEntityTypeConfiguration<ExperienceFeaturedImage>
{
    public void Configure(EntityTypeBuilder<ExperienceFeaturedImage> builder)
    {
        builder.ToTable("experience_featured_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Link)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.Link }).IsUnique();
    }
}
