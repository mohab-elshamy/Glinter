using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations;

public sealed class ExperienceModerationEventConfiguration
    : IEntityTypeConfiguration<ExperienceModerationEvent>
{
    public void Configure(EntityTypeBuilder<ExperienceModerationEvent> builder)
    {
        builder.ToTable("experience_moderation_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.ExperienceId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });
        builder.HasOne(x => x.Experience)
            .WithMany(x => x.ModerationHistory)
            .HasForeignKey(x => x.ExperienceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
