using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Chats.Queries;

public class GetMyChatThreadsHandler
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityUserReadService _identityUserReadService;

    public GetMyChatThreadsHandler(
        IChatThreadRepository threadRepository,
        ICurrentUserService currentUserService,
        IIdentityUserReadService identityUserReadService)
    {
        _threadRepository = threadRepository;
        _currentUserService = currentUserService;
        _identityUserReadService = identityUserReadService;
    }

    public async Task<List<ChatThreadSummaryDto>> HandleAsync(
        GetMyChatThreadsQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (query.Page < 1 || query.Page > 10000)
            throw new ValidationException("Page must be between 1 and 10000.");

        if (query.PageSize < 1 || query.PageSize > 100)
            throw new ValidationException("PageSize must be between 1 and 100.");

        var threads = await _threadRepository.GetThreadSummariesForUserAsync(
            currentUserId,
            query.Page,
            query.PageSize,
            cancellationToken);
        var names = await _identityUserReadService.GetDisplayNamesAsync(
            threads.SelectMany(x => x.ParticipantUserIds),
            cancellationToken);
        foreach (var thread in threads)
        {
            thread.ParticipantDisplayNames = thread.ParticipantUserIds
                .Where(names.ContainsKey)
                .ToDictionary(x => x, x => names[x]);
        }

        return threads;
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
