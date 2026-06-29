using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Services;

public class SafetyIndexHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SafetyIndexOptions> options,
    ILogger<SafetyIndexHostedService> logger) : BackgroundService
{
    private readonly SafetyIndexOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startupDelay = TimeSpan.FromSeconds(Math.Max(0, options.HostedServiceStartupDelaySeconds));
        if (startupDelay > TimeSpan.Zero)
        {
            await Task.Delay(startupDelay, stoppingToken);
        }

        await RunInitialHistoricalCollectionAsync(stoppingToken);

        if (!options.EnableWeeklyService)
        {
            logger.LogInformation("Weekly safety index service is disabled by configuration");
            return;
        }

        await RunWeeklyRefreshAsync(stoppingToken);

        var interval = TimeSpan.FromHours(Math.Max(1, options.WeeklyRefreshIntervalHours));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunWeeklyRefreshAsync(stoppingToken);
        }
    }

    private async Task RunInitialHistoricalCollectionAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISafetyIndexService>();

            await service.RunInitialHistoricalCollectionAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Initial historical safety index hosted job failed");
        }
    }

    private async Task RunWeeklyRefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISafetyIndexService>();

            await service.RefreshWeeklyAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weekly safety index hosted job failed");
        }
    }
}
