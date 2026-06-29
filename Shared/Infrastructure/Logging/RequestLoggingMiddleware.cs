using System.Diagnostics;
using System.Security.Claims;
using Glinter.Shared.Infrastructure.Metrics;

namespace Glinter.Shared.Infrastructure.Logging;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly ApplicationMetrics _metrics;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        ApplicationMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _metrics.RequestStarted();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RequestCompleted(
                context.Request.Method,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation(
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms for user {UserId}; correlation {CorrelationId}, trace {TraceId}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds,
            userId ?? "anonymous",
            context.TraceIdentifier,
            Activity.Current?.TraceId.ToString());
    }
}
