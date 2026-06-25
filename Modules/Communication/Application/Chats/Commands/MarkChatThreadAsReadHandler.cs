using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Chats.Commands;

public class MarkChatThreadAsReadHandler
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly ICurrentUserService _currentUserService;

    public MarkChatThreadAsReadHandler(
        IChatThreadRepository threadRepository,
        ICurrentUserService currentUserService)
    {
        _threadRepository = threadRepository;
        _currentUserService = currentUserService;
    }

    public async Task HandleAsync(
        MarkChatThreadAsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (command.ThreadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.");

        var participant = await _threadRepository.GetParticipantForUpdateAsync(
            command.ThreadId,
            currentUserId,
            cancellationToken);

        if (participant is null)
            throw new UnauthorizedAccessException("User is not a participant in this chat thread.");

        participant.LastReadAtUtc = DateTime.UtcNow;

        await _threadRepository.UpdateParticipantAsync(participant, cancellationToken);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
