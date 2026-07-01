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

        builder.Property(x => x.ExternalReviewId)
            .HasMaxLength(200);

        builder.Property(x => x.ReviewerName)
            .HasMaxLength(250);

        builder.Property(x => x.SourceList)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.ExternalReviewId })
            .IsUnique();
        builder.HasIndex(x => x.PublishedAtDate);
        builder.HasIndex(x => x.Rating);
        builder.HasIndex(x => x.CreatedByUserId);
    }
}
