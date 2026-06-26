using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.LinkUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.SourceModule)
            .HasMaxLength(100);

        builder.Property(x => x.SourceEntityType)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.ReadAtUtc });
        builder.HasIndex(x => new { x.SourceModule, x.SourceEntityType, x.SourceEntityId });
    }
}
