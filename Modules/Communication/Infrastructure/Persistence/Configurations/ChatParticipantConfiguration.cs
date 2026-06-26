using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Configurations;

public class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        builder.ToTable("chat_participants");

        builder.HasKey(x => new { x.ThreadId, x.UserId });

        builder.Property(x => x.JoinedAtUtc)
            .IsRequired();

        builder.Property(x => x.IsMuted)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.LeftAtUtc });
    }
}
