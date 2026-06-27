using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertTravelerProfile;

public class UpsertTravelerProfileCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    private readonly UpsertTravelerProfileCommandValidator _validator = new();

    public UpsertTravelerProfileCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService,
        ProfileFollowStatsService profileFollowStatsService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
        _profileFollowStatsService = profileFollowStatsService;
    }

    public async Task<TravelerProfileResponse> HandleAsync(
        UpsertTravelerProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;
        var requestedInterestIds = command.InterestIds.Distinct().ToList();

        var interests = await _profilesDbContext.Interests
            .Where(x => requestedInterestIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (interests.Count != requestedInterestIds.Count)
            throw new ValidationException("One or more interests are invalid.");

        var profile = await _profilesDbContext.TravelerProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new TravelerProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = command.DisplayName,
                Bio = command.Bio,
                Nationality = command.Nationality,
                PreferredBudgetLevel = command.PreferredBudgetLevel,
                TravelStyle = command.TravelStyle,
                PreferredInterests = command.PreferredInterests,
                CreatedAtUtc = DateTime.UtcNow
            };

            _profilesDbContext.TravelerProfiles.Add(profile);
        }
        else
        {
            profile.DisplayName = command.DisplayName;
            profile.Bio = command.Bio;
            profile.Nationality = command.Nationality;
            profile.PreferredBudgetLevel = command.PreferredBudgetLevel;
            profile.TravelStyle = command.TravelStyle;
            profile.PreferredInterests = command.PreferredInterests;
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        var existingInterestIds = profile.Interests
            .Select(x => x.InterestId)
            .ToList();

        var interestsToRemove = profile.Interests
            .Where(x => !requestedInterestIds.Contains(x.InterestId))
            .ToList();

        if (interestsToRemove.Count > 0)
            _profilesDbContext.TravelerInterests.RemoveRange(interestsToRemove);

        var interestIdsToAdd = requestedInterestIds
            .Where(x => !existingInterestIds.Contains(x))
            .ToList();

        foreach (var interestId in interestIdsToAdd)
        {
            var interest = interests.First(x => x.Id == interestId);

            profile.Interests.Add(new TravelerInterest
            {
                TravelerProfileId = profile.Id,
                InterestId = interest.Id,
                Interest = interest
            });
        }

        await _profilesDbContext.SaveChangesAsync(cancellationToken);

        var stats = await _profileFollowStatsService.GetCountsAsync(
            profile.UserId,
            cancellationToken);

        return ProfilesMappings.ToTravelerProfileResponse(
            profile,
            stats.FollowersCount,
            stats.FollowingCount);
    }
}