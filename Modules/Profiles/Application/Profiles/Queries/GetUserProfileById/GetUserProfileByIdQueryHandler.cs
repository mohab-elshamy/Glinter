using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Microsoft.EntityFrameworkCore;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetUserProfileById;

public class GetUserProfileByIdQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserProfileByIdQueryHandler(
        IProfilesDbContext profilesDbContext,
        ProfileFollowStatsService profileFollowStatsService,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _profileFollowStatsService = profileFollowStatsService;
        _currentUserService = currentUserService;
    }

    public async Task<object> HandleAsync(
        GetUserProfileByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
            throw new ValidationException("User id is required.");

        var stats = await _profileFollowStatsService.GetCountsAsync(
            query.UserId,
            cancellationToken);

        var travelerProfile = await _profilesDbContext.TravelerProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (travelerProfile is not null)
        {
            return ProfilesMappings.ToTravelerProfileResponse(
                travelerProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        var localBuddyProfile = await _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (localBuddyProfile is not null)
        {
            var isFollowing = _currentUserService.UserId is { } currentUserId &&
                              await _profilesDbContext.UserFollows.AnyAsync(
                                  x => x.FollowerUserId == currentUserId &&
                                       x.FollowedUserId == query.UserId,
                                  cancellationToken);
            return ProfilesMappings.ToLocalBuddyProfileResponse(
                localBuddyProfile,
                stats.FollowersCount,
                stats.FollowingCount,
                isFollowing);
        }

        var hotelOwnerProfile = await _profilesDbContext.HotelOwnerProfiles
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (hotelOwnerProfile is not null)
        {
            return ProfilesMappings.ToHotelOwnerProfileResponse(
                hotelOwnerProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        var experienceProviderProfile = await _profilesDbContext.ExperienceProviderProfiles
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (experienceProviderProfile is not null)
        {
            return ProfilesMappings.ToExperienceProviderProfileResponse(
                experienceProviderProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        throw new NotFoundException("Profile not found.");
    }
}
