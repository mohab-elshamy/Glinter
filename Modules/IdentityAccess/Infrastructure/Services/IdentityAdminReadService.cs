using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Enums;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Services;

public sealed class IdentityAdminReadService(
    IdentityAccessDbContext dbContext) : IIdentityAdminReadService
{
    public async Task<IdentityAdminSnapshot> GetSnapshotAsync(
        DateTime auditSinceUtc,
        CancellationToken cancellationToken = default)
    {
        var usersByRole = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                group userRole by role.Name into grouped
                select new { Role = grouped.Key!, Count = grouped.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count, cancellationToken);
        return new IdentityAdminSnapshot(
            await dbContext.Users.CountAsync(cancellationToken),
            await dbContext.Users.CountAsync(x => x.IsActive, cancellationToken),
            await dbContext.Users.CountAsync(
                x => x.AccountReviewStatus == AccountReviewStatus.Pending,
                cancellationToken),
            usersByRole,
            await dbContext.AdminAuditEvents.CountAsync(
                x => x.CreatedAtUtc >= auditSinceUtc,
                cancellationToken));
    }
}
