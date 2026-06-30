using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public sealed class BuddyReviewConfiguration : IEntityTypeConfiguration<BuddyReview>
{
    public void Configure(EntityTypeBuilder<BuddyReview> builder)
    {
        builder.ToTable("buddy_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReviewText).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.HasIndex(x => new { x.LocalBuddyUserId, x.CreatedAtUtc });
        builder.HasOne(x => x.Booking)
            .WithOne(x => x.Review)
            .HasForeignKey<BuddyReview>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
