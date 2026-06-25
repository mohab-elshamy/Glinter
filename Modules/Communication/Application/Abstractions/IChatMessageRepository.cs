using Glinter.Modules.Communication.Domain.Entities;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface IChatMessageRepository
{
    Task<ChatMessage> AddAsync(
        ChatMessage message,
        CancellationToken cancellationToken = default);

    Task<List<ChatMessage>> GetByThreadIdAsync(
        Guid threadId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
