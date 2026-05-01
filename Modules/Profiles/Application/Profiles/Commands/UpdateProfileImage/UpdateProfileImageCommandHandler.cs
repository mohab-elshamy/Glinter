using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateProfileImage;

public class UpdateProfileImageCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    private readonly UpdateProfileImageCommandValidator _validator = new();

    public UpdateProfileImageCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService,
        ProfileFollowStatsService profileFollowStatsService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
        _profileFollowStatsService = profileFollowStatsService;
    }

    public async Task<object> HandleAsync(
        UpdateProfileImageCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;

        var stats = await _profileFollowStatsService.GetCountsAsync(userId, cancellationToken);

        var travelerProfile = await _profilesDbContext.TravelerProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (travelerProfile is not null)
        {
            travelerProfile.ProfileImageUrl = command.ProfileImageUrl;
            travelerProfile.UpdatedAtUtc = DateTime.UtcNow;

            await _profilesDbContext.SaveChangesAsync(cancellationToken);

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
            localBuddyProfile.ProfileImageUrl = command.ProfileImageUrl;
            localBuddyProfile.UpdatedAtUtc = DateTime.UtcNow;

            await _profilesDbContext.SaveChangesAsync(cancellationToken);

            return ProfilesMappings.ToLocalBuddyProfileResponse(
                localBuddyProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        var hotelOwnerProfile = await _profilesDbContext.HotelOwnerProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (hotelOwnerProfile is not null)
        {
            hotelOwnerProfile.ProfileImageUrl = command.ProfileImageUrl;
            hotelOwnerProfile.UpdatedAtUtc = DateTime.UtcNow;

            await _profilesDbContext.SaveChangesAsync(cancellationToken);

            return ProfilesMappings.ToHotelOwnerProfileResponse(
                hotelOwnerProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        var experienceProviderProfile = await _profilesDbContext.ExperienceProviderProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (experienceProviderProfile is not null)
        {
            experienceProviderProfile.ProfileImageUrl = command.ProfileImageUrl;
            experienceProviderProfile.UpdatedAtUtc = DateTime.UtcNow;

            await _profilesDbContext.SaveChangesAsync(cancellationToken);

            return ProfilesMappings.ToExperienceProviderProfileResponse(
                experienceProviderProfile,
                stats.FollowersCount,
                stats.FollowingCount);
        }

        throw new KeyNotFoundException("Profile not found. Create your profile first.");
    }
}