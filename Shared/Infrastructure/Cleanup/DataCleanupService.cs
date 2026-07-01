using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

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
        await using var lockConnection = new NpgsqlConnection(
            _identity.Database.GetConnectionString());

        await lockConnection.OpenAsync(cancellationToken);

        if (!await TryAcquireLockAsync(
                lockConnection,
                cancellationToken))
        {
            return DataCleanupResult.Skipped;
        }

        try
        {
            return await RunWithLockAsync(cancellationToken);
        }
        finally
        {
            await ReleaseLockAsync(lockConnection);
        }
    }

    private async Task<DataCleanupResult> RunWithLockAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var revokedTokens = await TableExistsAsync(
            _identity,
            "revoked_tokens",
            cancellationToken)
            ? await DeleteInBatchesAsync(
                _identity,
                () => _identity.RevokedTokens
                    .Where(x =>
                        x.ExpiresAtUtc <
                        now.AddDays(
                            -_options.RevokedTokenRetentionDays))
                    .OrderBy(x => x.Id),
                cancellationToken)
            : 0;

        var refreshTokens = await TableExistsAsync(
            _identity,
            "refresh_tokens",
            cancellationToken)
            ? await DeleteInBatchesAsync(
                _identity,
                () => _identity.RefreshTokens
                    .Where(x =>
                        x.ExpiresAtUtc <
                        now.AddDays(
                            -_options.RefreshTokenRetentionDays) ||
                        (
                            x.RevokedAtUtc != null &&
                            x.RevokedAtUtc <
                            now.AddDays(
                                -_options.RefreshTokenRetentionDays)
                        ))
                    .OrderBy(x => x.Id),
                cancellationToken)
            : 0;

        var mfaChallenges = await TableExistsAsync(
            _identity,
            "mfa_challenges",
            cancellationToken)
            ? await DeleteInBatchesAsync(
                _identity,
                () => _identity.MfaChallenges
                    .Where(x =>
                        x.ExpiresAtUtc <
                        now.AddDays(
                            -_options.MfaChallengeRetentionDays) ||
                        (
                            x.ConsumedAtUtc != null &&
                            x.ConsumedAtUtc <
                            now.AddDays(
                                -_options.MfaChallengeRetentionDays)
                        ))
                    .OrderBy(x => x.Id),
                cancellationToken)
            : 0;

        var adminAuditEvents = await TableExistsAsync(
            _identity,
            "admin_audit_events",
            cancellationToken)
            ? await DeleteInBatchesAsync(
                _identity,
                () => _identity.AdminAuditEvents
                    .Where(x =>
                        x.CompletedAtUtc != null &&
                        x.CreatedAtUtc <
                        now.AddDays(
                            -_options.AdminAuditRetentionDays))
                    .OrderBy(x => x.Id),
                cancellationToken)
            : 0;

        var notificationsExist = await TableExistsAsync(
            _communication,
            "notifications",
            cancellationToken);

        var readNotifications = notificationsExist
            ? await DeleteInBatchesAsync(
                _communication,
                () => _communication.Notifications
                    .Where(x =>
                        x.ReadAtUtc != null &&
                        x.ReadAtUtc <
                        now.AddDays(
                            -_options.ReadNotificationRetentionDays))
                    .OrderBy(x => x.Id),
                cancellationToken)
            : 0;

        var unreadNotifications = notificationsExist
            ? await DeleteInBatchesAsync(
                _communication,
                () => _communication.Notifications
                    .Where(x =>
                        x.ReadAtUtc == null &&
                        x.CreatedAtUtc <
                        now.AddDays(
                            -_options.UnreadNotificationRetentionDays))
                    .OrderBy(x => x.Id),
                cancellationToken)
            : 0;

        return new DataCleanupResult(
            revokedTokens,
            refreshTokens,
            mfaChallenges,
            readNotifications,
            unreadNotifications,
            adminAuditEvents);
    }

    private async Task<int> DeleteInBatchesAsync<TEntity>(
        DbContext dbContext,
        Func<IQueryable<TEntity>> queryFactory,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var totalDeleted = 0;

        for (
            var batch = 0;
            batch < _options.MaxBatchesPerRun;
            batch++)
        {
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            var deleted = await queryFactory()
                .Take(_options.BatchSize)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            totalDeleted += deleted;

            if (deleted < _options.BatchSize)
            {
                break;
            }
        }

        return totalDeleted;
    }

    private static async Task<bool> TryAcquireLockAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT pg_try_advisory_lock(
                hashtextextended('glinter:data-cleanup', 0))
            """;

        return (bool)(
            await command.ExecuteScalarAsync(cancellationToken)
            ?? false);
    }

    private static async Task ReleaseLockAsync(
        NpgsqlConnection connection)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT pg_advisory_unlock(
                hashtextextended('glinter:data-cleanup', 0))
            """;

        await command.ExecuteScalarAsync(
            CancellationToken.None);
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
    int UnreadNotifications,
    int AdminAuditEvents)
{
    public static DataCleanupResult Skipped { get; } =
        new(0, 0, 0, 0, 0, 0)
        {
            SkippedDueToLock = true
        };

    public bool SkippedDueToLock { get; init; }

    public int TotalDeleted =>
        RevokedTokens +
        RefreshTokens +
        MfaChallenges +
        ReadNotifications +
        UnreadNotifications +
        AdminAuditEvents;
}