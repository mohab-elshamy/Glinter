using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceReviewConfiguration : IEntityTypeConfiguration<ExperienceReview>
{
    public void Configure(EntityTypeBuilder<ExperienceReview> builder)
    {
        builder.ToTable("experience_reviews");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ExperienceId, x.TravelerProfileId })
            .IsUnique();

        builder.Property(x => x.Rating)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Experience)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}