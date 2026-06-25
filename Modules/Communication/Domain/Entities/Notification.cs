using Glinter.Modules.Communication.Domain.Enums;

namespace Glinter.Modules.Communication.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationType Type { get; set; } = NotificationType.System;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public string? SourceModule { get; set; }

    public string? SourceEntityType { get; set; }

    public Guid? SourceEntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }
}
