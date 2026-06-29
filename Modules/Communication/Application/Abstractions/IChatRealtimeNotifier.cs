using Glinter.Modules.Communication.Application.Chats.Dtos;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface IChatRealtimeNotifier
{
    Task MessageCreatedAsync(
        ChatMessageEventDto message,
        CancellationToken cancellationToken = default);

    Task ThreadReadAsync(
        ChatThreadReadEventDto readEvent,
        CancellationToken cancellationToken = default);
}
