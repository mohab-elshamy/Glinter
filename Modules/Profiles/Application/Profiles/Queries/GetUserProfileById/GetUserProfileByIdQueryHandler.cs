using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetUserProfileById;

public class GetUserProfileByIdQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;

    public GetUserProfileByIdQueryHandler(IProfilesDbContext profilesDbContext)
    {
        _profilesDbContext = profilesDbContext;
    }

    public async Task<ProfileResponse> HandleAsync(
        GetUserProfileByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
            throw new InvalidOperationException("User id is required.");

        var travelerProfile = await _profilesDbContext.TravelerProfiles
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (travelerProfile is not null)
            return ProfilesMappings.ToProfileResponse(travelerProfile);

        var localBuddyProfile = await _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (localBuddyProfile is not null)
            return ProfilesMappings.ToProfileResponse(localBuddyProfile);

        var hotelOwnerProfile = await _profilesDbContext.HotelOwnerProfiles
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (hotelOwnerProfile is not null)
            return ProfilesMappings.ToProfileResponse(hotelOwnerProfile);

        var experienceProviderProfile = await _profilesDbContext.ExperienceProviderProfiles
            .FirstOrDefaultAsync(x => x.UserId == query.UserId, cancellationToken);

        if (experienceProviderProfile is not null)
            return ProfilesMappings.ToProfileResponse(experienceProviderProfile);

        throw new KeyNotFoundException("Profile not found.");
    }
}