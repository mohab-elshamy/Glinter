using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Common.Mapping;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetInterests;

public class GetInterestsQueryHandler
{
    private readonly IProfilesDbContext _profilesDbContext;

    public GetInterestsQueryHandler(IProfilesDbContext profilesDbContext)
    {
        _profilesDbContext = profilesDbContext;
    }

    public async Task<List<InterestResponse>> HandleAsync(
        GetInterestsQuery query,
        CancellationToken cancellationToken = default)
    {
        var interests = await _profilesDbContext.Interests
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return interests
            .Select(ProfilesMappings.ToInterestResponse)
            .ToList();
    }
}