using Glinter.Modules.Communication.Domain.Enums;

namespace Glinter.Modules.Communication.Domain.Entities;

public class ChatThread
{
    public Guid Id { get; set; }

    public ChatThreadType Type { get; set; } = ChatThreadType.Direct;

    public string? Title { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAtUtc { get; set; }

    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
