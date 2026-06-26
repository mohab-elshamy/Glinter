using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.SentAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.ThreadId, x.SentAtUtc });
        builder.HasIndex(x => x.SenderUserId);
    }
}
