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
}
