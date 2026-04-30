using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.FollowUser;

public class FollowUserCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly FollowUserCommandValidator _validator = new();

    public FollowUserCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<string> HandleAsync(
        FollowUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var followerUserId = _currentUserService.UserId.Value;

        if (followerUserId == command.FollowedUserId)
            throw new InvalidOperationException("You cannot follow yourself.");

        var targetProfileExists =
            await _profilesDbContext.TravelerProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken)
            || await _profilesDbContext.LocalBuddyProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken)
            || await _profilesDbContext.HotelOwnerProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken)
            || await _profilesDbContext.ExperienceProviderProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken);

        if (!targetProfileExists)
            throw new KeyNotFoundException("The user profile you want to follow was not found.");

        var alreadyFollowing = await _profilesDbContext.UserFollows
            .AnyAsync(
                x => x.FollowerUserId == followerUserId &&
                     x.FollowedUserId == command.FollowedUserId,
                cancellationToken);

        if (alreadyFollowing)
            return "You are already following this user.";

        _profilesDbContext.UserFollows.Add(new UserFollow
        {
            FollowerUserId = followerUserId,
            FollowedUserId = command.FollowedUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _profilesDbContext.SaveChangesAsync(cancellationToken);

        return "User followed successfully.";
    }
}