namespace Glinter.Modules.Communication.Application.Chats.Dtos;

public class ChatMessageResponseDto
{
    public Guid Id { get; set; }

    public Guid ThreadId { get; set; }

    public Guid SenderUserId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }

    public bool IsMine { get; set; }
}
