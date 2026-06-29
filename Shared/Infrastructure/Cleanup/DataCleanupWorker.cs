using Glinter.Shared.Infrastructure.Metrics;
using Microsoft.Extensions.Options;

namespace Glinter.Shared.Infrastructure.Cleanup;

public sealed class DataCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DataCleanupOptions _options;
    private readonly ApplicationMetrics _metrics;
    private readonly ILogger<DataCleanupWorker> _logger;

    public DataCleanupWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<DataCleanupOptions> options,
        ApplicationMetrics metrics,
        ILogger<DataCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_options.IntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCleanupAsync(stoppingToken);
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cleanup = scope.ServiceProvider.GetRequiredService<DataCleanupService>();
            var result = await cleanup.RunOnceAsync(cancellationToken);
            if (result.SkippedDueToLock)
            {
                _logger.LogInformation(
                    "Cleanup skipped because another instance holds the advisory lock.");
                return;
            }

            _metrics.CleanupCompleted(result.TotalDeleted);
            _logger.LogInformation(
                "Cleanup completed; deleted {TotalDeleted} rows ({RevokedTokens} revoked tokens, {RefreshTokens} refresh tokens, {MfaChallenges} MFA challenges, {ReadNotifications} read notifications, {UnreadNotifications} unread notifications, {AdminAuditEvents} admin audit events).",
                result.TotalDeleted,
                result.RevokedTokens,
                result.RefreshTokens,
                result.MfaChallenges,
                result.ReadNotifications,
                result.UnreadNotifications,
                result.AdminAuditEvents);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _metrics.CleanupFailed();
            _logger.LogError(exception, "Background data cleanup failed.");
        }
    }
}
