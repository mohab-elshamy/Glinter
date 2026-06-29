using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayReviewsPerRatingConfiguration : IEntityTypeConfiguration<StayReviewsPerRating>
{
    public void Configure(EntityTypeBuilder<StayReviewsPerRating> builder)
    {
        builder.ToTable("stay_reviews_per_rating");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.StayId);
        builder.HasIndex(x => new { x.StayId, x.Rating }).IsUnique();
    }
}
