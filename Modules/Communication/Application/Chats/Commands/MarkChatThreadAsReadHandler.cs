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
            throw new ValidationException("ThreadId is required.");

        var thread = await _threadRepository.GetByIdWithParticipantsAsync(
            command.ThreadId,
            cancellationToken);

        if (thread is null)
            throw new NotFoundException("Chat thread was not found.");

        var isParticipant = thread.Participants
            .Any(x => x.UserId == currentUserId && x.LeftAtUtc == null);

        if (!isParticipant)
            throw new ForbiddenException("You are not a participant in this chat thread.");

        var participant = await _threadRepository.GetParticipantForUpdateAsync(
            command.ThreadId,
            currentUserId,
            cancellationToken);

        if (participant is null)
            throw new ConflictException("Chat participation changed while the request was being processed.");

        participant.LastReadAtUtc = DateTime.UtcNow;

        await _threadRepository.UpdateParticipantAsync(participant, cancellationToken);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
