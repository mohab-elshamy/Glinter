using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public sealed class ExperienceFavoriteConfiguration : IEntityTypeConfiguration<ExperienceFavorite>
{
    public void Configure(EntityTypeBuilder<ExperienceFavorite> builder)
    {
        builder.ToTable("experience_favorites");
        builder.HasKey(x => new { x.UserId, x.ExperienceId });
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}
