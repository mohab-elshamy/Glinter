using Glinter.Modules.Communication.Domain.Enums;

namespace Glinter.Modules.Communication.Application.Notifications.Dtos;

public class NotificationResponseDto
{
    public Guid Id { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public string? SourceModule { get; set; }

    public string? SourceEntityType { get; set; }

    public Guid? SourceEntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public bool IsRead { get; set; }
}
