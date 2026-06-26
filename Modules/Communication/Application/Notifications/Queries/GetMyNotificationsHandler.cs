using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Notifications.Queries;

public class GetMyNotificationsHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<NotificationsPageResponseDto> HandleAsync(
        GetMyNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (query.Page < 1 || query.Page > 10000)
            throw new ArgumentException("Page must be between 1 and 10000.");

        if (query.PageSize < 1 || query.PageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100.");

        var page = query.Page;
        var pageSize = query.PageSize;
        var skip = (page - 1) * pageSize;

        var notifications = await _notificationRepository.GetByUserIdAsync(
            currentUserId,
            skip,
            pageSize,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(
            currentUserId,
            cancellationToken);

        return new NotificationsPageResponseDto
        {
            Items = notifications
                .Select(CommunicationMappings.ToNotificationResponseDto)
                .ToList(),
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
