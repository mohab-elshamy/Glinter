using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Glinter.Modules.Profiles.Application.Common.Services;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertLocalBuddyProfile;

public class UpsertLocalBuddyProfileCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UpsertLocalBuddyProfileCommandValidator _validator = new();
    private readonly ProfileFollowStatsService _profileFollowStatsService;

    public UpsertLocalBuddyProfileCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService,
        ProfileFollowStatsService profileFollowStatsService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
        _profileFollowStatsService = profileFollowStatsService;
    }

    public async Task<LocalBuddyProfileResponse> HandleAsync(
        UpsertLocalBuddyProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;
        var requestedInterestIds = command.InterestIds.Distinct().ToList();

        var interests = await _profilesDbContext.Interests
            .Where(x => requestedInterestIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (interests.Count != requestedInterestIds.Count)
            throw new InvalidOperationException("One or more interests are invalid.");

        var profile = await _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new LocalBuddyProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = command.DisplayName,
                Bio = command.Bio,
                City = command.City,
                Languages = command.Languages,
                Rating = 0,
                ReviewsCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            _profilesDbContext.LocalBuddyProfiles.Add(profile);
        }
        else
        {
            profile.DisplayName = command.DisplayName;
            profile.Bio = command.Bio;
            profile.City = command.City;
            profile.Languages = command.Languages;
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        var existingInterestIds = profile.Interests.Select(x => x.InterestId).ToList();

        var interestsToRemove = profile.Interests
            .Where(x => !requestedInterestIds.Contains(x.InterestId))
            .ToList();

        if (interestsToRemove.Count > 0)
            _profilesDbContext.BuddyInterests.RemoveRange(interestsToRemove);

        var interestIdsToAdd = requestedInterestIds
            .Where(x => !existingInterestIds.Contains(x))
            .ToList();

        foreach (var interestId in interestIdsToAdd)
        {
            var interest = interests.First(x => x.Id == interestId);

            profile.Interests.Add(new BuddyInterest
            {
                LocalBuddyProfileId = profile.Id,
                InterestId = interest.Id,
                Interest = interest
            });
        }

        await _profilesDbContext.SaveChangesAsync(cancellationToken);

        var stats = await _profileFollowStatsService.GetCountsAsync(
            profile.UserId,
            cancellationToken);

        return ProfilesMappings.ToLocalBuddyProfileResponse(
            profile,
            stats.FollowersCount,
            stats.FollowingCount);
    }
}