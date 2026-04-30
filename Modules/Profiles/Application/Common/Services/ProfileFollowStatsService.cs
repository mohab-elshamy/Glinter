using Glinter.Modules.Profiles.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Common.Services;

public sealed record ProfileFollowStats(int FollowersCount, int FollowingCount);

public class ProfileFollowStatsService
{
    private readonly IProfilesDbContext _profilesDbContext;

    public ProfileFollowStatsService(IProfilesDbContext profilesDbContext)
    {
        _profilesDbContext = profilesDbContext;
    }

    public async Task<ProfileFollowStats> GetCountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var followersCount = await _profilesDbContext.UserFollows
            .CountAsync(x => x.FollowedUserId == userId, cancellationToken);

        var followingCount = await _profilesDbContext.UserFollows
            .CountAsync(x => x.FollowerUserId == userId, cancellationToken);

        return new ProfileFollowStats(followersCount, followingCount);
    }

    public async Task<Dictionary<Guid, ProfileFollowStats>> GetCountsForUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds
            .Distinct()
            .ToList();

        var followersCounts = await _profilesDbContext.UserFollows
            .Where(x => ids.Contains(x.FollowedUserId))
            .GroupBy(x => x.FollowedUserId)
            .Select(x => new
            {
                UserId = x.Key,
                Count = x.Count()
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        var followingCounts = await _profilesDbContext.UserFollows
            .Where(x => ids.Contains(x.FollowerUserId))
            .GroupBy(x => x.FollowerUserId)
            .Select(x => new
            {
                UserId = x.Key,
                Count = x.Count()
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        return ids.ToDictionary(
            id => id,
            id => new ProfileFollowStats(
                followersCounts.GetValueOrDefault(id),
                followingCounts.GetValueOrDefault(id)));
    }
}