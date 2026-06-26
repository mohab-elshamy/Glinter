namespace Glinter.Modules.Communication.Application.Chats.Commands;

public class SendChatMessageCommand
{
    public Guid ThreadId { get; set; }

    public string Body { get; set; } = string.Empty;
}
