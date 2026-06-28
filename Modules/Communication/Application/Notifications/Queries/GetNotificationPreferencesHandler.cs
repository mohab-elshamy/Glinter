using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Notifications.Queries;

public sealed class GetNotificationPreferencesHandler
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationPreferencesHandler(
        INotificationPreferenceRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationPreferenceResponseDto> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var preference = await _repository.GetAsync(GetCurrentUserId(), cancellationToken);
        return preference is null
            ? new NotificationPreferenceResponseDto
            {
                InAppEnabled = true,
                ChatMessageNotificationsEnabled = true,
                SystemNotificationsEnabled = true
            }
            : new NotificationPreferenceResponseDto
            {
                InAppEnabled = preference.InAppEnabled,
                EmailEnabled = preference.EmailEnabled,
                PushEnabled = preference.PushEnabled,
                ChatMessageNotificationsEnabled =
                    preference.ChatMessageNotificationsEnabled,
                SystemNotificationsEnabled = preference.SystemNotificationsEnabled,
                UpdatedAtUtc = preference.UpdatedAtUtc
            };
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        return _currentUserService.UserId.Value;
    }
}
