using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

public sealed class SignalRChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRChatRealtimeNotifier> _logger;

    public SignalRChatRealtimeNotifier(
        IHubContext<ChatHub> hubContext,
        ILogger<SignalRChatRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
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
            await _hubContext.Clients
                .Group(ChatHubGroups.Thread(threadId))
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
