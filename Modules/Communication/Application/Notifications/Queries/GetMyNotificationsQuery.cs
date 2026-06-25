namespace Glinter.Modules.Communication.Application.Notifications.Queries;

public class GetMyNotificationsQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
