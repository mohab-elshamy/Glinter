namespace Glinter.Modules.Communication.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public ChatThread Thread { get; set; } = null!;

    public Guid SenderUserId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
