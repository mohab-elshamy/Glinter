using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceAmenityConfiguration : IEntityTypeConfiguration<ExperienceAmenity>
{
    public void Configure(EntityTypeBuilder<ExperienceAmenity> builder)
    {
        builder.ToTable("experience_amenities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.Name }).IsUnique();
    }
}
