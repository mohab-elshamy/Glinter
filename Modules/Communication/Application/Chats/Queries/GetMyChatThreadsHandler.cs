using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Chats.Queries;

public class GetMyChatThreadsHandler
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyChatThreadsHandler(
        IChatThreadRepository threadRepository,
        ICurrentUserService currentUserService)
    {
        _threadRepository = threadRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<ChatThreadSummaryDto>> HandleAsync(
        GetMyChatThreadsQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var threads = await _threadRepository.GetThreadsForUserAsync(
            currentUserId,
            cancellationToken);

        return threads
            .Select(x => CommunicationMappings.ToThreadSummaryDto(x, currentUserId))
            .ToList();
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
