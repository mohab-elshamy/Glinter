using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Common.Services;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;

public class GetLocalBuddiesQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;
    private readonly ProfileFollowStatsService _profileFollowStatsService;
    private readonly ICurrentUserService _currentUserService;

    public GetLocalBuddiesQueryHandler(
        IProfilesDbContext profilesDbContext,
        ProfileFollowStatsService profileFollowStatsService,
        ICurrentUserService currentUserService)
    {
        _profilesDbContext = profilesDbContext;
        _profileFollowStatsService = profileFollowStatsService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<LocalBuddyListItemResponse>> HandleAsync(
        GetLocalBuddiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        if (pageSize > 50)
            pageSize = 50;

        var localBuddiesQuery = _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .Where(x => x.VerificationStatus == VerificationStatus.Approved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();

            localBuddiesQuery = localBuddiesQuery
                .Where(x => EF.Functions.ILike(x.City, city));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search.Trim()}%";

            localBuddiesQuery = localBuddiesQuery
                .Where(x =>
                    EF.Functions.ILike(x.DisplayName, searchPattern) ||
                    (x.Bio != null && EF.Functions.ILike(x.Bio, searchPattern)) ||
                    EF.Functions.ILike(x.City, searchPattern) ||
                    (x.Languages != null && EF.Functions.ILike(x.Languages, searchPattern)) ||
                    x.Interests.Any(i => EF.Functions.ILike(i.Interest.Name, searchPattern)));
        }

        var totalCount = await localBuddiesQuery.CountAsync(cancellationToken);

        var localBuddies = await localBuddiesQuery
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.ReviewsCount)
            .ThenBy(x => x.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var statsByUserId = await _profileFollowStatsService.GetCountsForUsersAsync(
            localBuddies.Select(x => x.UserId),
            cancellationToken);
        var followedUserIds = _currentUserService.UserId is { } currentUserId
            ? await _profilesDbContext.UserFollows
                .Where(x => x.FollowerUserId == currentUserId &&
                            localBuddies.Select(b => b.UserId).Contains(x.FollowedUserId))
                .Select(x => x.FollowedUserId)
                .ToHashSetAsync(cancellationToken)
            : [];

        var items = localBuddies
            .Select(profile =>
            {
                var stats = statsByUserId.GetValueOrDefault(
                    profile.UserId,
                    new ProfileFollowStats(0, 0));

                return ProfilesMappings.ToLocalBuddyListItemResponse(
                    profile,
                    stats.FollowersCount,
                    stats.FollowingCount,
                    followedUserIds.Contains(profile.UserId));
            })
            .ToList();

        return new PagedResponse<LocalBuddyListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
