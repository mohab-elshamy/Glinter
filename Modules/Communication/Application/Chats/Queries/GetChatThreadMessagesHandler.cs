using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Chats.Queries;

public class GetChatThreadMessagesHandler
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetChatThreadMessagesHandler(
        IChatThreadRepository threadRepository,
        IChatMessageRepository messageRepository,
        ICurrentUserService currentUserService)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<ChatMessageResponseDto>> HandleAsync(
        GetChatThreadMessagesQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (query.ThreadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.");

        var thread = await _threadRepository.GetByIdWithParticipantsAsync(
            query.ThreadId,
            cancellationToken);

        if (thread is null)
            throw new KeyNotFoundException("Chat thread was not found.");

        var isParticipant = thread.Participants
            .Any(x => x.UserId == currentUserId && x.LeftAtUtc == null);

        if (!isParticipant)
            throw new KeyNotFoundException("Chat thread was not found.");

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : Math.Min(query.PageSize, 100);
        var skip = (page - 1) * pageSize;

        var messages = await _messageRepository.GetByThreadIdAsync(
            query.ThreadId,
            skip,
            pageSize,
            cancellationToken);

        return messages
            .OrderBy(x => x.SentAtUtc)
            .Select(x => CommunicationMappings.ToMessageResponseDto(x, currentUserId))
            .ToList();
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
