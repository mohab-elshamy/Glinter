using Glinter.Modules.SafetyIndex.Application.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.External.Groq;

public class GroqRequestRateLimiter(
    IOptionsMonitor<SafetyIndexOptions> options,
    ILogger<GroqRequestRateLimiter> logger)
{
    private readonly object syncRoot = new();
    private DateTimeOffset nextAvailableAtUtc = DateTimeOffset.MinValue;

    public void Defer(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        lock (syncRoot)
        {
            var deferredUntil = DateTimeOffset.UtcNow.Add(delay);
            if (nextAvailableAtUtc < deferredUntil)
            {
                nextAvailableAtUtc = deferredUntil;
            }
        }
    }

    public async Task WaitAsync(CancellationToken ct = default)
    {
        var requestsPerMinute = options.CurrentValue.GroqRequestsPerMinute;
        if (requestsPerMinute <= 0)
        {
            return;
        }

        var interval = TimeSpan.FromMilliseconds(60_000d / requestsPerMinute);
        TimeSpan waitTime;

        lock (syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            if (nextAvailableAtUtc < now)
            {
                nextAvailableAtUtc = now;
            }

            waitTime = nextAvailableAtUtc - now;
            nextAvailableAtUtc = nextAvailableAtUtc.Add(interval);
        }

        if (waitTime <= TimeSpan.Zero)
        {
            return;
        }

        logger.LogInformation(
            "Waiting {WaitMilliseconds} ms before sending the next Groq request to respect {RequestsPerMinute} requests/minute",
            Math.Round(waitTime.TotalMilliseconds),
            requestsPerMinute);

        await Task.Delay(waitTime, ct);
    }
}
