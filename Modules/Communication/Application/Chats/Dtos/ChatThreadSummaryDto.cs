using Glinter.Modules.Communication.Domain.Enums;

namespace Glinter.Modules.Communication.Application.Chats.Dtos;

public class ChatThreadSummaryDto
{
    public Guid Id { get; set; }

    public ChatThreadType Type { get; set; }

    public string? Title { get; set; }

    public List<Guid> ParticipantUserIds { get; set; } = [];
    public Dictionary<Guid, string> ParticipantDisplayNames { get; set; } = [];

    public string? LastMessageBody { get; set; }

    public Guid? LastMessageSenderUserId { get; set; }

    public DateTime? LastMessageAtUtc { get; set; }

    public int UnreadCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
