using Glinter.Modules.Communication.Application.Notifications.Dtos;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface INotificationRealtimeNotifier
{
    Task NotificationCreatedAsync(
        Guid userId,
        NotificationResponseDto notification,
        CancellationToken cancellationToken = default);
}
