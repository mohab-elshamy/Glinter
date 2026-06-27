using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;

public class UpdateLocalBuddyVerificationCommandHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    private readonly UpdateLocalBuddyVerificationCommandValidator _validator = new();

    public UpdateLocalBuddyVerificationCommandHandler(
        IProfilesDbContext profilesDbContext,
        ProfileFollowStatsService profileFollowStatsService)
    {
        _profilesDbContext = profilesDbContext;
        _profileFollowStatsService = profileFollowStatsService;
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

        profile.VerificationStatus = verificationStatus;
        profile.UpdatedAtUtc = DateTime.UtcNow;

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