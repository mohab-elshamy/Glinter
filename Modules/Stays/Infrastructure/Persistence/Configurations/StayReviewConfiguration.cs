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

        builder.Property(x => x.Comment)
            .HasMaxLength(2000);

        builder.Property(x => x.Rating)
            .IsRequired();
    }
}