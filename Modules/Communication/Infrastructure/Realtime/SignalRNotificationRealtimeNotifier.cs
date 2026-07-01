using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

public sealed class SignalRNotificationRealtimeNotifier(
    IHubContext<ChatHub> hubContext,
    IIdentityUserReadService userReadService,
    ITokenRevocationService tokenRevocationService,
    ChatConnectionRegistry connectionRegistry,
    ILogger<SignalRNotificationRealtimeNotifier> logger)
    : INotificationRealtimeNotifier
{
    public async Task NotificationCreatedAsync(
        Guid userId,
        NotificationResponseDto notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var registrations = connectionRegistry.GetRegistrationsForUser(userId);
            if (registrations.Count == 0)
                return;

            var validConnectionIds = new List<string>(registrations.Count);
            foreach (var registration in registrations)
            {
                var valid = registration.ExpiresAtUtc > DateTime.UtcNow &&
                            await userReadService.IsActiveUserWithSecurityStampAsync(
                                registration.UserId,
                                registration.SecurityStamp,
                                cancellationToken) &&
                            !await tokenRevocationService.IsRevokedAsync(
                                registration.Jti,
                                cancellationToken);
                if (valid)
                    validConnectionIds.Add(registration.ConnectionId);
                else
                    connectionRegistry.RemoveConnection(registration.ConnectionId);
            }

            if (validConnectionIds.Count > 0)
            {
                await hubContext.Clients
                    .Clients(validConnectionIds)
                    .SendAsync("NotificationCreated", notification, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "SignalR delivery failed for notification {NotificationId}.",
                notification.Id);
        }
    }
}
