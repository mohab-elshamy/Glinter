using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.Communication.Domain.Entities;

namespace Glinter.Modules.Communication.Application.Notifications.Commands;

public class CreateNotificationHandler
{
    private readonly INotificationRepository _notificationRepository;

    public CreateNotificationHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationResponseDto> HandleAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        var title = command.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.");

        if (title.Length > 200)
            throw new ArgumentException("Notification title cannot exceed 200 characters.");

        var body = command.Body.Trim();
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Notification body is required.");

        if (body.Length > 1000)
            throw new ArgumentException("Notification body cannot exceed 1000 characters.");

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            Type = command.Type,
            Title = title,
            Body = body,
            LinkUrl = string.IsNullOrWhiteSpace(command.LinkUrl) ? null : command.LinkUrl.Trim(),
            SourceModule = string.IsNullOrWhiteSpace(command.SourceModule) ? null : command.SourceModule.Trim(),
            SourceEntityType = string.IsNullOrWhiteSpace(command.SourceEntityType) ? null : command.SourceEntityType.Trim(),
            SourceEntityId = command.SourceEntityId,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createdNotification = await _notificationRepository.AddAsync(
            notification,
            cancellationToken);

        return CommunicationMappings.ToNotificationResponseDto(createdNotification);
    }
}
