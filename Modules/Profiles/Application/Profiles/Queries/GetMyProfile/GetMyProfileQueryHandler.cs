using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetMyProfile;

public class GetMyProfileQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProfileQueryHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<ProfileResponse> HandleAsync(
        GetMyProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;

        var travelerProfile = await _profilesDbContext.TravelerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (travelerProfile is not null)
            return ProfilesMappings.ToProfileResponse(travelerProfile);

        var localBuddyProfile = await _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (localBuddyProfile is not null)
            return ProfilesMappings.ToProfileResponse(localBuddyProfile);

        var hotelOwnerProfile = await _profilesDbContext.HotelOwnerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (hotelOwnerProfile is not null)
            return ProfilesMappings.ToProfileResponse(hotelOwnerProfile);

        var experienceProviderProfile = await _profilesDbContext.ExperienceProviderProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (experienceProviderProfile is not null)
            return ProfilesMappings.ToProfileResponse(experienceProviderProfile);

        throw new KeyNotFoundException("Profile not found.");
    }
}