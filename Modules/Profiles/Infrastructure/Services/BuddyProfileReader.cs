using Glinter.Modules.Buddy.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Enums;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Infrastructure.Services;

public sealed class BuddyProfileReader(ProfilesDbContext dbContext)
    : IBuddyProfileReader
{
    public async Task<TravelerProfileSummary?> GetTravelerAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.TravelerProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new TravelerProfileSummary(x.UserId, x.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<LocalBuddyProfileSummary?> GetLocalBuddyAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.LocalBuddyProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new LocalBuddyProfileSummary(
                x.UserId,
                x.DisplayName,
                x.VerificationStatus == VerificationStatus.Approved))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>>
        GetTravelerDisplayNamesAsync(
            IEnumerable<Guid> userIds,
            CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await dbContext.TravelerProfiles.AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .ToDictionaryAsync(
                x => x.UserId,
                x => x.DisplayName,
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>>
        GetLocalBuddyDisplayNamesAsync(
            IEnumerable<Guid> userIds,
            CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await dbContext.LocalBuddyProfiles.AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .ToDictionaryAsync(
                x => x.UserId,
                x => x.DisplayName,
                cancellationToken);
    }

    public async Task UpdateLocalBuddyReviewSummaryAsync(
        Guid buddyUserId,
        decimal averageRating,
        int reviewsCount,
        CancellationToken cancellationToken)
    {
        var buddy = await dbContext.LocalBuddyProfiles.SingleOrDefaultAsync(
            x => x.UserId == buddyUserId,
            cancellationToken)
            ?? throw new NotFoundException("Local buddy profile not found.");
        buddy.Rating = averageRating;
        buddy.ReviewsCount = reviewsCount;
        buddy.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
