using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

public sealed class SignalRChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IChatThreadRepository _threadRepository;
    private readonly IIdentityUserReadService _userReadService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly ChatConnectionRegistry _connectionRegistry;
    private readonly ILogger<SignalRChatRealtimeNotifier> _logger;

    public SignalRChatRealtimeNotifier(
        IHubContext<ChatHub> hubContext,
        IChatThreadRepository threadRepository,
        IIdentityUserReadService userReadService,
        ITokenRevocationService tokenRevocationService,
        ChatConnectionRegistry connectionRegistry,
        ILogger<SignalRChatRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _threadRepository = threadRepository;
        _userReadService = userReadService;
        _tokenRevocationService = tokenRevocationService;
        _connectionRegistry = connectionRegistry;
        _logger = logger;
    }

    public Task MessageCreatedAsync(
        ChatMessageEventDto message,
        CancellationToken cancellationToken = default) =>
        SendSafelyAsync(
            message.ThreadId,
            "MessageReceived",
            message,
            cancellationToken);

    public Task ThreadReadAsync(
        ChatThreadReadEventDto readEvent,
        CancellationToken cancellationToken = default) =>
        SendSafelyAsync(
            readEvent.ThreadId,
            "ThreadRead",
            readEvent,
            cancellationToken);

    private async Task SendSafelyAsync<T>(
        Guid threadId,
        string method,
        T payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var thread = await _threadRepository.GetByIdWithParticipantsAsync(
                threadId,
                cancellationToken);
            if (thread is null)
                return;

            var participantUserIds = thread.Participants
                .Where(x => x.LeftAtUtc == null)
                .Select(x => x.UserId)
                .Distinct()
                .ToArray();
            var registrations = _connectionRegistry.GetRegistrations(
                threadId,
                participantUserIds.ToHashSet());

            if (registrations.Count == 0)
                return;

            var validConnectionIds = new List<string>(registrations.Count);
            foreach (var registration in registrations)
            {
                var valid = registration.ExpiresAtUtc > DateTime.UtcNow &&
                            await _userReadService.IsActiveUserWithSecurityStampAsync(
                                registration.UserId,
                                registration.SecurityStamp,
                                cancellationToken) &&
                            !await _tokenRevocationService.IsRevokedAsync(
                                registration.Jti,
                                cancellationToken);

                if (valid)
                    validConnectionIds.Add(registration.ConnectionId);
                else
                    _connectionRegistry.RemoveConnection(registration.ConnectionId);
            }

            if (validConnectionIds.Count == 0)
                return;

            await _hubContext.Clients
                .Clients(validConnectionIds)
                .SendAsync(method, payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "SignalR delivery failed for chat thread {ThreadId} and event {EventName}.",
                threadId,
                method);
        }
    }
}
