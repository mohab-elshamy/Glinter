using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Services;

public class IdentityUserReadService : IIdentityUserReadService
{
    private readonly IdentityAccessDbContext _dbContext;

    public IdentityUserReadService(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return false;

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }

    public async Task<bool> IsActiveUserWithSecurityStampAsync(
        Guid userId,
        string securityStamp,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(securityStamp))
            return false;

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == userId &&
                     x.IsActive &&
                     x.SecurityStamp == securityStamp,
                cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetActiveUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var distinctUserIds = userIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinctUserIds.Length == 0)
            return new HashSet<Guid>();

        var activeUserIds = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && distinctUserIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return activeUserIds.ToHashSet();
    }
}
