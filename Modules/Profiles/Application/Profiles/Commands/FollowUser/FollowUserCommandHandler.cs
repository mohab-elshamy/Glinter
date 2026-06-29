using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            throw new ValidationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var followerUserId = _currentUserService.UserId.Value;

        if (followerUserId == command.FollowedUserId)
            throw new ValidationException("You cannot follow yourself.");

        var targetProfileExists =
            await _profilesDbContext.TravelerProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken)
            || await _profilesDbContext.LocalBuddyProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken)
            || await _profilesDbContext.HotelOwnerProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken)
            || await _profilesDbContext.ExperienceProviderProfiles.AnyAsync(x => x.UserId == command.FollowedUserId, cancellationToken);

        if (!targetProfileExists)
            throw new NotFoundException("The user profile you want to follow was not found.");

        var alreadyFollowing = await _profilesDbContext.UserFollows
            .AnyAsync(
                x => x.FollowerUserId == followerUserId &&
                     x.FollowedUserId == command.FollowedUserId,
                cancellationToken);

        if (alreadyFollowing)
            return "You are already following this user.";

        var follow = new UserFollow
        {
            FollowerUserId = followerUserId,
            FollowedUserId = command.FollowedUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _profilesDbContext.UserFollows.Add(follow);

        try
        {
            await _profilesDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation,
                      ConstraintName: "PK_user_follows"
                  })
        {
            return "You are already following this user.";
        }

        return "User followed successfully.";
    }
}
