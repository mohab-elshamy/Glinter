using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public sealed class StayFavoriteConfiguration : IEntityTypeConfiguration<StayFavorite>
{
    public void Configure(EntityTypeBuilder<StayFavorite> builder)
    {
        builder.ToTable("stay_favorites");
        builder.HasKey(x => new { x.UserId, x.StayId });
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        builder.HasOne(x => x.Stay)
            .WithMany()
            .HasForeignKey(x => x.StayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
