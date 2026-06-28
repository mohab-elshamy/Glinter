using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Glinter.Shared.Infrastructure.Cleanup;

public sealed class DataCleanupService
{
    private readonly IdentityAccessDbContext _identity;
    private readonly CommunicationDbContext _communication;
    private readonly DataCleanupOptions _options;

    public DataCleanupService(
        IdentityAccessDbContext identity,
        CommunicationDbContext communication,
        IOptions<DataCleanupOptions> options)
    {
        _identity = identity;
        _communication = communication;
        _options = options.Value;
    }

    public async Task<DataCleanupResult> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await using var identityTransaction =
            await _identity.Database.BeginTransactionAsync(cancellationToken);

        var revokedTokens = await TableExistsAsync(
            _identity,
            "revoked_tokens",
            cancellationToken)
            ? await _identity.RevokedTokens
                .Where(x => x.ExpiresAtUtc <
                            now.AddDays(-_options.RevokedTokenRetentionDays))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;
        var refreshTokens = await TableExistsAsync(
            _identity,
            "refresh_tokens",
            cancellationToken)
            ? await _identity.RefreshTokens
                .Where(x =>
                    x.ExpiresAtUtc <
                    now.AddDays(-_options.RefreshTokenRetentionDays) ||
                    (x.RevokedAtUtc != null &&
                     x.RevokedAtUtc <
                     now.AddDays(-_options.RefreshTokenRetentionDays)))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;
        var mfaChallenges = await TableExistsAsync(
            _identity,
            "mfa_challenges",
            cancellationToken)
            ? await _identity.MfaChallenges
                .Where(x =>
                    x.ExpiresAtUtc <
                    now.AddDays(-_options.MfaChallengeRetentionDays) ||
                    (x.ConsumedAtUtc != null &&
                     x.ConsumedAtUtc <
                     now.AddDays(-_options.MfaChallengeRetentionDays)))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;
        await identityTransaction.CommitAsync(cancellationToken);

        await using var communicationTransaction =
            await _communication.Database.BeginTransactionAsync(cancellationToken);
        var notificationsExist = await TableExistsAsync(
            _communication,
            "notifications",
            cancellationToken);
        var readNotifications = notificationsExist
            ? await _communication.Notifications
                .Where(x => x.ReadAtUtc != null &&
                            x.ReadAtUtc <
                            now.AddDays(-_options.ReadNotificationRetentionDays))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;
        var unreadNotifications = notificationsExist
            ? await _communication.Notifications
                .Where(x => x.ReadAtUtc == null &&
                            x.CreatedAtUtc <
                            now.AddDays(-_options.UnreadNotificationRetentionDays))
                .ExecuteDeleteAsync(cancellationToken)
            : 0;
        await communicationTransaction.CommitAsync(cancellationToken);

        return new DataCleanupResult(
            revokedTokens,
            refreshTokens,
            mfaChallenges,
            readNotifications,
            unreadNotifications);
    }

    private static async Task<bool> TableExistsAsync(
        DbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        var qualifiedTableName = $"public.{tableName}";
        return await dbContext.Database
            .SqlQuery<bool>(
                $"SELECT to_regclass({qualifiedTableName}) IS NOT NULL AS \"Value\"")
            .SingleAsync(cancellationToken);
    }
}

public sealed record DataCleanupResult(
    int RevokedTokens,
    int RefreshTokens,
    int MfaChallenges,
    int ReadNotifications,
    int UnreadNotifications)
{
    public int TotalDeleted =>
        RevokedTokens +
        RefreshTokens +
        MfaChallenges +
        ReadNotifications +
        UnreadNotifications;
}
