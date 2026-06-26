namespace Glinter.Modules.Communication.Application.Chats.Queries;

public class GetChatThreadMessagesQuery
{
    public Guid ThreadId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
