using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Services;

public sealed class SafetyIndexHostedService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SafetyIndexOptions _options;
    private readonly ILogger<SafetyIndexHostedService> _logger;

    public SafetyIndexHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<SafetyIndexOptions> options,
        ILogger<SafetyIndexHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            var initialCollectionEnabled =
                _options.RunInitialHistoricalCollectionOnStartup;

            var weeklyServiceEnabled =
                _options.EnableWeeklyService;

            if (!initialCollectionEnabled &&
                !weeklyServiceEnabled)
            {
                _logger.LogInformation(
                    "Safety Index background collection is disabled. Existing stored safety results remain available.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    _options.GroqApiKey))
            {
                _logger.LogWarning(
                    "Safety Index background collection was requested, but SafetyIndex:GroqApiKey is missing. The collection jobs will not run.");

                return;
            }

            var startupDelay = TimeSpan.FromSeconds(
                Math.Max(
                    0,
                    _options.HostedServiceStartupDelaySeconds));

            if (startupDelay > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "Safety Index background service will start after {StartupDelaySeconds} seconds.",
                    startupDelay.TotalSeconds);

                await Task.Delay(
                    startupDelay,
                    stoppingToken);
            }

            if (initialCollectionEnabled)
            {
                await RunInitialHistoricalCollectionAsync(
                    stoppingToken);
            }

            if (!weeklyServiceEnabled)
            {
                _logger.LogInformation(
                    "Weekly Safety Index refresh is disabled.");

                return;
            }

            /*
             * Run once when the weekly service starts so a deployed
             * environment does not need to wait seven days for its
             * first refresh.
             */
            await RunWeeklyRefreshAsync(stoppingToken);

            var interval = TimeSpan.FromHours(
                Math.Max(
                    1,
                    _options.WeeklyRefreshIntervalHours));

            _logger.LogInformation(
                "Weekly Safety Index refresh is enabled with an interval of {IntervalHours} hours.",
                interval.TotalHours);

            using var timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await RunWeeklyRefreshAsync(
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Safety Index hosted service stopped.");
        }
        catch (Exception exception)
        {
            /*
             * Prevent an external API/background-job failure from
             * terminating the complete ASP.NET Core application.
             */
            _logger.LogError(
                exception,
                "Safety Index hosted service stopped after an unexpected error.");
        }
    }

    private async Task RunInitialHistoricalCollectionAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var safetyIndexService =
                scope.ServiceProvider
                    .GetRequiredService<ISafetyIndexService>();

            _logger.LogInformation(
                "Starting initial historical Safety Index collection.");

            await safetyIndexService
                .RunInitialHistoricalCollectionAsync(
                    cancellationToken);

            _logger.LogInformation(
                "Initial historical Safety Index collection completed.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Initial historical Safety Index collection failed.");
        }
    }

    private async Task RunWeeklyRefreshAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var safetyIndexService =
                scope.ServiceProvider
                    .GetRequiredService<ISafetyIndexService>();

            _logger.LogInformation(
                "Starting weekly Safety Index refresh.");

            await safetyIndexService.RefreshWeeklyAsync(
                cancellationToken);

            _logger.LogInformation(
                "Weekly Safety Index refresh completed.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Weekly Safety Index refresh failed.");
        }
    }
}