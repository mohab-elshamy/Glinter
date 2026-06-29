using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.Communication.Application.Chats.Dtos;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface IChatThreadRepository
{
    Task<ChatThreadSummaryDto?> GetDirectThreadSummaryAsync(
        string directKey,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<ChatThread?> GetByIdWithParticipantsAsync(
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<List<ChatThreadSummaryDto>> GetThreadSummariesForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ChatParticipant?> GetParticipantForUpdateAsync(
        Guid threadId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatThread> AddAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default);

    Task<ChatThread?> TryAddDirectThreadAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default);

    Task UpdateParticipantAsync(
        ChatParticipant participant,
        CancellationToken cancellationToken = default);
}
