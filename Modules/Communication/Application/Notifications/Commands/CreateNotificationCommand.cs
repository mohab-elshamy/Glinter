using Glinter.Modules.Communication.Domain.Enums;

namespace Glinter.Modules.Communication.Application.Notifications.Commands;

public class CreateNotificationCommand
{
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public string? SourceModule { get; set; }

    public string? SourceEntityType { get; set; }

    public Guid? SourceEntityId { get; set; }
}
