using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Notifications.Commands;

public class CreateNotificationHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IIdentityUserReadService _identityUserReadService;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<CreateNotificationHandler> _logger;

    public CreateNotificationHandler(
        INotificationRepository notificationRepository,
        IIdentityUserReadService identityUserReadService,
        INotificationPreferenceRepository preferenceRepository,
        INotificationRealtimeNotifier realtimeNotifier,
        ILogger<CreateNotificationHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _identityUserReadService = identityUserReadService;
        _preferenceRepository = preferenceRepository;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<NotificationResponseDto?> HandleAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            throw new ValidationException("UserId is required.");

        bool userExists;
        try
        {
            userExists = await _identityUserReadService.IsActiveUserAsync(
                command.UserId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Could not validate notification recipient {UserId}.",
                command.UserId);
            return null;
        }

        if (!userExists)
            return null;

        var title = command.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Notification title is required.");

        if (title.Length > 200)
            throw new ValidationException("Notification title cannot exceed 200 characters.");

        var body = command.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
            throw new ValidationException("Notification body is required.");

        if (body.Length > 1000)
            throw new ValidationException("Notification body cannot exceed 1000 characters.");

        if (!Enum.IsDefined(command.Type))
            throw new ValidationException("Notification type is invalid.");

        bool inAppEnabled;
        try
        {
            inAppEnabled = await _preferenceRepository.IsInAppEnabledAsync(
                command.UserId,
                command.Type,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Could not read notification preferences for user {UserId}.",
                command.UserId);
            return null;
        }
        if (!inAppEnabled)
        {
            return null;
        }

        var linkUrl = ValidateAndNormalizeLink(command.LinkUrl);
        var sourceModule = NormalizeOptionalValue(command.SourceModule, 100, "SourceModule");
        var sourceEntityType = NormalizeOptionalValue(
            command.SourceEntityType,
            100,
            "SourceEntityType");

        if (command.SourceEntityId == Guid.Empty)
            throw new ValidationException("SourceEntityId must be a valid id.");

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            Type = command.Type,
            Title = title,
            Body = body,
            LinkUrl = linkUrl,
            SourceModule = sourceModule,
            SourceEntityType = sourceEntityType,
            SourceEntityId = command.SourceEntityId,
            CreatedAtUtc = DateTime.UtcNow
        };

        Notification createdNotification;
        try
        {
            createdNotification = await _notificationRepository.AddAsync(
                notification,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Business operations call notifications after committing their own
            // transaction. A notification-store outage must not turn a committed
            // booking/message/moderation action into a false client failure.
            _logger.LogError(
                exception,
                "Could not persist {NotificationType} notification for user {UserId}.",
                command.Type,
                command.UserId);
            return null;
        }

        var response = CommunicationMappings.ToNotificationResponseDto(createdNotification);
        try
        {
            await _realtimeNotifier.NotificationCreatedAsync(
                command.UserId,
                response,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The persisted notification remains available for polling even when
            // the transient realtime delivery path is unavailable.
            _logger.LogWarning(
                exception,
                "Notification {NotificationId} was persisted but realtime delivery failed.",
                createdNotification.Id);
        }
        return response;
    }

    private static string? ValidateAndNormalizeLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var link = value.Trim();

        if (link.Length > 1000)
            throw new ValidationException("Notification link cannot exceed 1000 characters.");

        if (!link.StartsWith('/') ||
            link.StartsWith("//", StringComparison.Ordinal) ||
            link.Contains('\\') ||
            link.Any(char.IsControl) ||
            !Uri.TryCreate(link, UriKind.Relative, out _))
        {
            throw new ValidationException(
                "Notification link must be a safe application-relative path.");
        }

        string decodedLink;
        try
        {
            decodedLink = Uri.UnescapeDataString(link);
        }
        catch (UriFormatException)
        {
            throw new ValidationException("Notification link contains invalid escaping.");
        }

        if (decodedLink.StartsWith("//", StringComparison.Ordinal) ||
            decodedLink.Contains('\\') ||
            decodedLink.Any(char.IsControl))
        {
            throw new ValidationException(
                "Notification link must be a safe application-relative path.");
        }

        ValidateApplicationDestination(link);
        return link;
    }

    private static void ValidateApplicationDestination(string link)
    {
        if (link.Contains('#'))
            throw new ValidationException("Notification links cannot contain fragments.");

        var separator = link.IndexOf('?');
        var path = separator < 0 ? link : link[..separator];
        var query = separator < 0 ? string.Empty : link[(separator + 1)..];
        var parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query);

        var simplePaths = new HashSet<string>(StringComparer.Ordinal)
        {
            "/explore",
            "/local-buddies",
            "/where-to-stay",
            "/where-to-go"
        };
        if (simplePaths.Contains(path))
        {
            if (parameters.Count != 0)
                throw new ValidationException("Notification link contains unsupported parameters.");
            return;
        }

        if (path.StartsWith("/profile/", StringComparison.Ordinal) &&
            Guid.TryParse(path["/profile/".Length..], out _) &&
            parameters.Count == 0)
        {
            return;
        }

        if (path == "/messages")
        {
            if (parameters.Keys.Any(x => x != "thread") ||
                parameters.TryGetValue("thread", out var threadValues) &&
                (threadValues.Count != 1 || !Guid.TryParse(threadValues[0], out _)))
            {
                throw new ValidationException("Notification message link is invalid.");
            }
            return;
        }

        if (path == "/profile/me")
        {
            var allowedTabs = new HashSet<string>(StringComparer.Ordinal)
            {
                "bookings", "hotels", "experiences", "buddy-schedule"
            };
            if (parameters.Keys.Any(x => x != "tab") ||
                parameters.TryGetValue("tab", out var tabValues) &&
                (tabValues.Count != 1 || !allowedTabs.Contains(tabValues[0]!)))
            {
                throw new ValidationException("Notification profile link is invalid.");
            }
            return;
        }

        throw new ValidationException("Notification link does not target a supported application page.");
    }

    private static string? NormalizeOptionalValue(
        string? value,
        int maxLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ValidationException($"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }
}
