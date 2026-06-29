using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetMyProfile;

public class GetMyProfileQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProfileFollowStatsService _profileFollowStatsService;

    public GetMyProfileQueryHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService,
        ProfileFollowStatsService profileFollowStatsService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
        _profileFollowStatsService = profileFollowStatsService;
    }

    public async Task<object> HandleAsync(
        GetMyProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;
        var stats = await _profileFollowStatsService.GetCountsAsync(userId, cancellationToken);

        var travelerProfile = await _profilesDbContext.TravelerProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

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
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (localBuddyProfile is not null)
        {
            return ProfilesMappings.ToLocalBuddyProfileResponse(
                localBuddyProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        var hotelOwnerProfile = await _profilesDbContext.HotelOwnerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (hotelOwnerProfile is not null)
        {
            return ProfilesMappings.ToHotelOwnerProfileResponse(
                hotelOwnerProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        var experienceProviderProfile = await _profilesDbContext.ExperienceProviderProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

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