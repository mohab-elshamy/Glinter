using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;

public class GetLocalBuddiesQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;

    public GetLocalBuddiesQueryHandler(IProfilesDbContext profilesDbContext)
    {
        _profilesDbContext = profilesDbContext;
    }

    public async Task<List<LocalBuddyListItemResponse>> HandleAsync(
        GetLocalBuddiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var localBuddiesQuery = _profilesDbContext.LocalBuddyProfiles
            .Include(x => x.Interests)
            .ThenInclude(x => x.Interest)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            localBuddiesQuery = localBuddiesQuery
                .Where(x => x.City.ToLower() == query.City.ToLower());
        }

        var localBuddies = await localBuddiesQuery
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.ReviewsCount)
            .ToListAsync(cancellationToken);

        return localBuddies
            .Select(ProfilesMappings.ToLocalBuddyListItemResponse)
            .ToList();
    }
}