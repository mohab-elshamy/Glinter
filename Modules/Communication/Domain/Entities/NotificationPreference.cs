namespace Glinter.Modules.Communication.Domain.Entities;

public class NotificationPreference
{
    public Guid UserId { get; set; }

    public bool InAppEnabled { get; set; } = true;

    public bool EmailEnabled { get; set; }

    public bool PushEnabled { get; set; }

    public bool ChatMessageNotificationsEnabled { get; set; } = true;

    public bool SystemNotificationsEnabled { get; set; } = true;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
