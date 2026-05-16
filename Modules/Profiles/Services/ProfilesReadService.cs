using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Infrastructure.Services;

public class ProfilesReadService : IProfilesReadService
{
    private readonly ProfilesDbContext _dbContext;

    public ProfilesReadService(ProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetTravelerProfileIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.TravelerProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetHotelOwnerProfileIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.HotelOwnerProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetLocalBuddyProfileIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.LocalBuddyProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetExperienceProviderProfileIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ExperienceProviderProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}