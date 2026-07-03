using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Notifications.Commands;

public sealed class UpdateNotificationPreferencesHandler
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateNotificationPreferencesHandler(
        INotificationPreferenceRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationPreferenceResponseDto> HandleAsync(
        UpdateNotificationPreferenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var preference = await _repository.UpsertAsync(
            new NotificationPreference
            {
                UserId = GetCurrentUserId(),
                InAppEnabled = request.InAppEnabled,
                // Email and push delivery providers are not configured yet. Keep
                // these channels disabled instead of persisting misleading settings.
                EmailEnabled = false,
                PushEnabled = false,
                ChatMessageNotificationsEnabled =
                    request.ChatMessageNotificationsEnabled,
                SystemNotificationsEnabled = request.SystemNotificationsEnabled,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        return new NotificationPreferenceResponseDto
        {
            InAppEnabled = preference.InAppEnabled,
            EmailEnabled = false,
            PushEnabled = false,
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
