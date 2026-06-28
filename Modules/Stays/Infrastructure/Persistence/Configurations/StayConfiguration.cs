using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Configurations;

public class StayConfiguration : IEntityTypeConfiguration<Stay>
{
    public void Configure(EntityTypeBuilder<Stay> builder)
    {
        builder.ToTable("stays");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Adm3Gid);
        builder.HasIndex(x => new { x.IsActive, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Adm3Gid, x.IsActive, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.IsActive, x.Currency, x.PricePerNight });
        builder.HasIndex(x => new { x.OwnerProfileId, x.CreatedAtUtc });

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.PricePerNight)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(x => new
        {
            x.OwnerProfileId,
            x.Name,
            x.Address
        }).IsUnique();
    }
}
