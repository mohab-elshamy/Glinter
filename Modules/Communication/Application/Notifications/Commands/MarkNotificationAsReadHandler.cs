using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Notifications.Commands;

public class MarkNotificationAsReadHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public MarkNotificationAsReadHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationResponseDto> HandleAsync(
        MarkNotificationAsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (command.NotificationId == Guid.Empty)
            throw new ValidationException("NotificationId is required.");

        var notification = await _notificationRepository.GetByIdForUserAsync(
            command.NotificationId,
            currentUserId,
            cancellationToken);

        if (notification is null)
            throw new NotFoundException("Notification was not found.");

        notification.ReadAtUtc ??= DateTime.UtcNow;

        await _notificationRepository.UpdateAsync(notification, cancellationToken);

        return CommunicationMappings.ToNotificationResponseDto(notification);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
