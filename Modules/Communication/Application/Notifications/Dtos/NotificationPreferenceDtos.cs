namespace Glinter.Modules.Communication.Application.Notifications.Dtos;

public sealed class NotificationPreferenceResponseDto
{
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool ChatMessageNotificationsEnabled { get; set; }
    public bool SystemNotificationsEnabled { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class UpdateNotificationPreferenceRequestDto
{
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool ChatMessageNotificationsEnabled { get; set; }
    public bool SystemNotificationsEnabled { get; set; }
}
