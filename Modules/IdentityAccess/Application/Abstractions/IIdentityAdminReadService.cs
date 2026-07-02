namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface IIdentityAdminReadService
{
    Task<IdentityAdminSnapshot> GetSnapshotAsync(
        DateTime auditSinceUtc,
        CancellationToken cancellationToken = default);
}

public sealed record IdentityAdminSnapshot(
    int TotalUsers,
    int ActiveUsers,
    IReadOnlyDictionary<string, int> UsersByRole,
    int RecentAuditEvents);
