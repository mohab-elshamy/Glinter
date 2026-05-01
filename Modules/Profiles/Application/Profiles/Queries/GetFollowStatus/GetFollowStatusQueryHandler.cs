using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetFollowStatus;

public class GetFollowStatusQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetFollowStatusQueryHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<FollowStatusResponse> HandleAsync(
        GetFollowStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (query.FollowedUserId == Guid.Empty)
            throw new InvalidOperationException("Followed user id is required.");

        var followerUserId = _currentUserService.UserId.Value;

        var isFollowing = await _profilesDbContext.UserFollows
            .AnyAsync(
                x => x.FollowerUserId == followerUserId &&
                     x.FollowedUserId == query.FollowedUserId,
                cancellationToken);

        return new FollowStatusResponse
        {
            FollowedUserId = query.FollowedUserId,
            IsFollowing = isFollowing
        };
    }
}