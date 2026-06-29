using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Entities;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;

public class UpdateLocalBuddyVerificationCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    private readonly UpdateLocalBuddyVerificationCommandValidator _validator = new();
    private readonly ICurrentUserService _currentUserService;

    public UpdateLocalBuddyVerificationCommandHandler(
        IProfilesDbContext profilesDbContext,
        ProfileFollowStatsService profileFollowStatsService,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _profileFollowStatsService = profileFollowStatsService;
        _currentUserService = currentUserService;
    }

    public async Task<LocalBuddyProfileResponse> HandleAsync(
        UpdateLocalBuddyVerificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);

        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        var profile = await _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .FirstOrDefaultAsync(x => x.UserId == command.UserId, cancellationToken);

        if (profile is null)
            throw new NotFoundException("Local buddy profile not found.");

        var verificationStatus = Enum.Parse<VerificationStatus>(
            command.VerificationStatus,
            ignoreCase: true);

        var previousStatus = profile.VerificationStatus;
        profile.VerificationStatus = verificationStatus;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        _profilesDbContext.LocalBuddyVerificationEvents.Add(
            new LocalBuddyVerificationEvent
            {
                Id = Guid.NewGuid(),
                LocalBuddyUserId = profile.UserId,
                ActorUserId = GetCurrentUserId(),
                PreviousStatus = previousStatus,
                NewStatus = verificationStatus,
                Notes = string.IsNullOrWhiteSpace(command.ModerationNotes)
                    ? null
                    : command.ModerationNotes.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });

        await _profilesDbContext.SaveChangesAsync(cancellationToken);

        var stats = await _profileFollowStatsService.GetCountsAsync(
            profile.UserId,
            cancellationToken);

        return ProfilesMappings.ToLocalBuddyProfileResponse(
            profile,
            stats.FollowersCount,
            stats.FollowingCount);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        return _currentUserService.UserId.Value;
    }
}
