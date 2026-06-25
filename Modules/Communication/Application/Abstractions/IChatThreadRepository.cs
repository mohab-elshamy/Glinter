using Glinter.Modules.Communication.Domain.Entities;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface IChatThreadRepository
{
    Task<ChatThread?> GetDirectThreadAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default);

    Task<ChatThread?> GetByIdWithParticipantsAsync(
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<ChatThread?> GetByIdWithParticipantsAndMessagesAsync(
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<List<ChatThread>> GetThreadsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatParticipant?> GetParticipantForUpdateAsync(
        Guid threadId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatThread> AddAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default);

    Task UpdateParticipantAsync(
        ChatParticipant participant,
        CancellationToken cancellationToken = default);
}
