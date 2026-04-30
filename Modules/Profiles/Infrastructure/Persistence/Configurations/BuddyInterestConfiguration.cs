using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public class BuddyInterestConfiguration : IEntityTypeConfiguration<BuddyInterest>
{
    public void Configure(EntityTypeBuilder<BuddyInterest> builder)
    {
        builder.ToTable("buddy_interests");

        builder.HasKey(x => new { x.LocalBuddyProfileId, x.InterestId });

        builder.HasOne(x => x.LocalBuddyProfile)
            .WithMany(x => x.Interests)
            .HasForeignKey(x => x.LocalBuddyProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Interest)
            .WithMany(x => x.BuddyInterests)
            .HasForeignKey(x => x.InterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}