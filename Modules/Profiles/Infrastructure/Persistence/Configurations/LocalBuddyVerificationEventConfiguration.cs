using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Configurations;

public sealed class LocalBuddyVerificationEventConfiguration
    : IEntityTypeConfiguration<LocalBuddyVerificationEvent>
{
    public void Configure(EntityTypeBuilder<LocalBuddyVerificationEvent> builder)
    {
        builder.ToTable("local_buddy_verification_events");
        builder.HasKey(x => x.Id);
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
        builder.HasIndex(x => new { x.LocalBuddyUserId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });
    }
}
