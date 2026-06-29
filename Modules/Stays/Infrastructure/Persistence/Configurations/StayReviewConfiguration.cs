using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayReviewConfiguration : IEntityTypeConfiguration<StayReview>
{
    public void Configure(EntityTypeBuilder<StayReview> builder)
    {
        builder.ToTable("stay_reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalReviewId)
            .HasMaxLength(200);

        builder.Property(x => x.ReviewerName)
            .HasMaxLength(250);

        builder.Property(x => x.Platform)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.StayId);
        builder.HasIndex(x => new { x.StayId, x.ExternalReviewId }).IsUnique();
        builder.HasIndex(x => x.Platform);
        builder.HasIndex(x => x.Rating);
        builder.HasIndex(x => x.PublishedAtDate);
    }
}
