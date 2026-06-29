namespace Glinter.Modules.Communication.Application.Chats.Dtos;

public sealed class ChatMessageEventDto
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}

public sealed class ChatThreadReadEventDto
{
    public Guid ThreadId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAtUtc { get; set; }
}
