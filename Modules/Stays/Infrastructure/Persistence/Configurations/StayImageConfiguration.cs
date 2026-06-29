using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayImageConfiguration : IEntityTypeConfiguration<StayImage>
{
    public void Configure(EntityTypeBuilder<StayImage> builder)
    {
        builder.ToTable("stay_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Link)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasIndex(x => x.StayId);
        builder.HasIndex(x => new { x.StayId, x.Link }).IsUnique();
    }
}
