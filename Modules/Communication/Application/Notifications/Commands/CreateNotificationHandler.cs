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

    public CreateNotificationHandler(
        INotificationRepository notificationRepository,
        IIdentityUserReadService identityUserReadService)
    {
        _notificationRepository = notificationRepository;
        _identityUserReadService = identityUserReadService;
    }

    public async Task<NotificationResponseDto> HandleAsync(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            throw new ArgumentException("UserId is required.");

        var userExists = await _identityUserReadService.IsActiveUserAsync(
            command.UserId,
            cancellationToken);

        if (!userExists)
            throw new KeyNotFoundException("User was not found or is inactive.");

        var title = command.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.");

        if (title.Length > 200)
            throw new ArgumentException("Notification title cannot exceed 200 characters.");

        var body = command.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Notification body is required.");

        if (body.Length > 1000)
            throw new ArgumentException("Notification body cannot exceed 1000 characters.");

        if (!Enum.IsDefined(command.Type))
            throw new ArgumentException("Notification type is invalid.");

        var linkUrl = ValidateAndNormalizeLink(command.LinkUrl);
        var sourceModule = NormalizeOptionalValue(command.SourceModule, 100, "SourceModule");
        var sourceEntityType = NormalizeOptionalValue(
            command.SourceEntityType,
            100,
            "SourceEntityType");

        if (command.SourceEntityId == Guid.Empty)
            throw new ArgumentException("SourceEntityId must be a valid id.");

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

        var createdNotification = await _notificationRepository.AddAsync(
            notification,
            cancellationToken);

        return CommunicationMappings.ToNotificationResponseDto(createdNotification);
    }

    private static string? ValidateAndNormalizeLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var link = value.Trim();

        if (link.Length > 1000)
            throw new ArgumentException("Notification link cannot exceed 1000 characters.");

        if (!link.StartsWith('/') ||
            link.StartsWith("//", StringComparison.Ordinal) ||
            link.Contains('\\') ||
            link.Any(char.IsControl) ||
            !Uri.TryCreate(link, UriKind.Relative, out _))
        {
            throw new ArgumentException(
                "Notification link must be a safe application-relative path.");
        }

        string decodedLink;
        try
        {
            decodedLink = Uri.UnescapeDataString(link);
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Notification link contains invalid escaping.");
        }

        if (decodedLink.StartsWith("//", StringComparison.Ordinal) ||
            decodedLink.Contains('\\') ||
            decodedLink.Any(char.IsControl))
        {
            throw new ArgumentException(
                "Notification link must be a safe application-relative path.");
        }

        return link;
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
            throw new ArgumentException($"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }
}
