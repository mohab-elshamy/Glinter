using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.Communication.Domain.Enums;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreference> UpsertAsync(
        NotificationPreference preference,
        CancellationToken cancellationToken = default);

    Task<bool> IsInAppEnabledAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default);
}
