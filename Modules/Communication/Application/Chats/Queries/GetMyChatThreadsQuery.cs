namespace Glinter.Modules.Communication.Application.Chats.Queries;

public class GetMyChatThreadsQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
