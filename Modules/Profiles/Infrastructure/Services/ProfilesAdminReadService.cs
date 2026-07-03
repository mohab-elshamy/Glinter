using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Enums;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Infrastructure.Services;

public sealed class ProfilesAdminReadService(
    ProfilesDbContext dbContext) : IProfilesAdminReadService
{
    public async Task<ProfilesAdminSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        new(
            await dbContext.LocalBuddyProfiles.CountAsync(
                x => x.VerificationStatus == VerificationStatus.Pending,
                cancellationToken),
            await dbContext.LocalBuddyProfiles.CountAsync(
                x => x.VerificationStatus == VerificationStatus.Approved,
                cancellationToken));
}
