using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Glinter.Modules.Profiles.Application.Common.Services;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertExperienceProviderProfile;

public class UpsertExperienceProviderProfileCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UpsertExperienceProviderProfileCommandValidator _validator = new();
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    
    public UpsertExperienceProviderProfileCommandHandler(
        IProfilesDbContext profilesDbContext,
        ICurrentUserService currentUserService,
        ProfileFollowStatsService profileFollowStatsService)
    {
        _profilesDbContext = profilesDbContext;
        _currentUserService = currentUserService;
        _profileFollowStatsService = profileFollowStatsService;
    }

    public async Task<ExperienceProviderProfileResponse> HandleAsync(
        UpsertExperienceProviderProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = _currentUserService.UserId.Value;

        var profile = await _profilesDbContext.ExperienceProviderProfiles
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new ExperienceProviderProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BusinessName = command.BusinessName,
                ContactPersonName = command.ContactPersonName,
                PhoneNumber = command.PhoneNumber,
                Description = command.Description,
                CreatedAtUtc = DateTime.UtcNow
            };

            _profilesDbContext.ExperienceProviderProfiles.Add(profile);
        }
        else
        {
            profile.BusinessName = command.BusinessName;
            profile.ContactPersonName = command.ContactPersonName;
            profile.PhoneNumber = command.PhoneNumber;
            profile.Description = command.Description;
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _profilesDbContext.SaveChangesAsync(cancellationToken);
        
        var stats = await _profileFollowStatsService.GetCountsAsync(
            profile.UserId,
            cancellationToken);
        return ProfilesMappings.ToExperienceProviderProfileResponse(
            profile,
            stats.FollowersCount,
            stats.FollowingCount);
    }
}