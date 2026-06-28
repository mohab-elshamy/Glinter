using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public class ExperienceConfiguration : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> builder)
    {
        builder.ToTable("experiences");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Adm3Gid);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new
        {
            x.IsActive,
            x.ApprovalStatus,
            x.CreatedAtUtc
        });
        builder.HasIndex(x => new
        {
            x.Adm3Gid,
            x.IsActive,
            x.ApprovalStatus
        });
        builder.HasIndex(x => new
        {
            x.CategoryId,
            x.IsActive,
            x.ApprovalStatus
        });
        builder.HasIndex(x => new { x.ProviderProfileId, x.CreatedAtUtc });
        builder.HasIndex(x => new
        {
            x.ProviderProfileId,
            x.Adm3Gid,
            x.Title
        }).IsUnique();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(3000)
            .IsRequired();

        builder.Property(x => x.LocationName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.PricePerPerson)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.DurationMinutes)
            .IsRequired();

        builder.Property(x => x.MaxGuests)
            .IsRequired();

        builder.Property(x => x.Latitude)
            .IsRequired();

        builder.Property(x => x.Longitude)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.ApprovalStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ModerationNotes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Experiences)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
