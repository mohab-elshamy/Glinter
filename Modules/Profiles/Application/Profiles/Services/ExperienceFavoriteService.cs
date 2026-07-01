using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Profiles.Services;

public sealed class ExperienceFavoriteService(
    IProfilesDbContext dbContext,
    ICurrentUserService currentUser)
{
    public Task<List<int>> GetIdsAsync(CancellationToken cancellationToken) =>
        dbContext.ExperienceFavorites
            .AsNoTracking()
            .Where(x => x.UserId == GetUserId())
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.ExperienceId)
            .ToListAsync(cancellationToken);

    public async Task<ExperienceFavoriteResponse> AddAsync(
        int experienceId,
        CancellationToken cancellationToken)
    {
        if (experienceId <= 0)
            throw new ValidationException("ExperienceId must be positive.");
        var userId = GetUserId();
        if (!await dbContext.ExperienceFavorites.AnyAsync(
                x => x.UserId == userId && x.ExperienceId == experienceId,
                cancellationToken))
        {
            dbContext.ExperienceFavorites.Add(new ExperienceFavorite
            {
                UserId = userId,
                ExperienceId = experienceId,
                CreatedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return new ExperienceFavoriteResponse(experienceId, true);
    }

    public async Task<ExperienceFavoriteResponse> RemoveAsync(
        int experienceId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var favorite = await dbContext.ExperienceFavorites.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ExperienceId == experienceId,
            cancellationToken);
        if (favorite is not null)
        {
            dbContext.ExperienceFavorites.Remove(favorite);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return new ExperienceFavoriteResponse(experienceId, false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await dbContext.ExperienceFavorites
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Guid GetUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        return currentUser.UserId.Value;
    }
}

public sealed record ExperienceFavoriteResponse(int ExperienceId, bool IsFavorite);
