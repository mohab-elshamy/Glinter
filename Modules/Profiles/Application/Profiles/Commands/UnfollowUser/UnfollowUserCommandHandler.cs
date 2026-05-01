using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UnfollowUser;

public class UnfollowUserCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UnfollowUserCommandValidator _validator = new();

    public UnfollowUserCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<string> HandleAsync(
        UnfollowUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var followerUserId = _currentUserService.UserId.Value;

        if (followerUserId == command.FollowedUserId)
            throw new InvalidOperationException("You cannot unfollow yourself.");

        var follow = await _profilesDbContext.UserFollows
            .FirstOrDefaultAsync(
                x => x.FollowerUserId == followerUserId &&
                     x.FollowedUserId == command.FollowedUserId,
                cancellationToken);

        if (follow is null)
            return "You are not following this user.";

        _profilesDbContext.UserFollows.Remove(follow);

        await _profilesDbContext.SaveChangesAsync(cancellationToken);

        return "User unfollowed successfully.";
    }
}