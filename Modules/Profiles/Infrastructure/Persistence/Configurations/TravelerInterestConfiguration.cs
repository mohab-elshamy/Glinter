using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public class TravelerInterestConfiguration : IEntityTypeConfiguration<TravelerInterest>
{
    public void Configure(EntityTypeBuilder<TravelerInterest> builder)
    {
        builder.ToTable("traveler_interests");

        builder.HasKey(x => new { x.TravelerProfileId, x.InterestId });

        builder.HasOne(x => x.TravelerProfile)
            .WithMany(x => x.Interests)
            .HasForeignKey(x => x.TravelerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Interest)
            .WithMany(x => x.TravelerInterests)
            .HasForeignKey(x => x.InterestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}