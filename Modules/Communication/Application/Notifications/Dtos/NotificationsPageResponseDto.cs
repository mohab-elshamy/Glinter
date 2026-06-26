namespace Glinter.Modules.Communication.Application.Notifications.Dtos;

public class NotificationsPageResponseDto
{
    public List<NotificationResponseDto> Items { get; set; } = [];

    public int UnreadCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
