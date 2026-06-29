using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceReviewsPerRatingConfiguration : IEntityTypeConfiguration<ExperienceReviewsPerRating>
{
    public void Configure(EntityTypeBuilder<ExperienceReviewsPerRating> builder)
    {
        builder.ToTable("experience_reviews_per_rating");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ExperienceId);
        builder.HasIndex(x => new { x.ExperienceId, x.Rating }).IsUnique();
    }
}
