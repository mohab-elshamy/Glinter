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
    private readonly ChatConnectionRegistry _connectionRegistry;
    private readonly ILogger<SignalRChatRealtimeNotifier> _logger;

    public SignalRChatRealtimeNotifier(
        IHubContext<ChatHub> hubContext,
        IChatThreadRepository threadRepository,
        IIdentityUserReadService userReadService,
        ChatConnectionRegistry connectionRegistry,
        ILogger<SignalRChatRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _threadRepository = threadRepository;
        _userReadService = userReadService;
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
            var activeUserIds = await _userReadService.GetActiveUserIdsAsync(
                participantUserIds,
                cancellationToken);
            var connectionIds = _connectionRegistry.GetConnections(
                threadId,
                activeUserIds);

            if (connectionIds.Count == 0)
                return;

            await _hubContext.Clients
                .Clients(connectionIds)
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
