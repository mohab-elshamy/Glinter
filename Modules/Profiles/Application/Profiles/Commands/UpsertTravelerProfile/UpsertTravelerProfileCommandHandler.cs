using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertTravelerProfile;

public class UpsertTravelerProfileCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UpsertTravelerProfileCommandValidator _validator = new();

    public UpsertTravelerProfileCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
    }

    public async Task<ProfileResponse> HandleAsync(
        UpsertTravelerProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;

        var profile = await _profilesDbContext.TravelerProfiles
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

        await _profilesDbContext.SaveChangesAsync(cancellationToken);

        return ProfilesMappings.ToProfileResponse(profile);
    }
}