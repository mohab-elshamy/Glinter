using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Shared.Application.Analytics;
using Glinter.Shared.Infrastructure.Analytics;
using Glinter.Shared.Infrastructure.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Shared.Presentation.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminOperationsController : ControllerBase
{
    [HttpGet("/api/admin/analytics")]
    public async Task<ActionResult<AdminAnalyticsDto>> GetAnalytics(
        [FromServices] AdminAnalyticsService analyticsService,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken) =>
        Ok(await analyticsService.GetAsync(from, to, cancellationToken));

    [HttpGet("/metrics")]
    [Produces("text/plain")]
    public ContentResult GetMetrics([FromServices] ApplicationMetrics metrics) =>
        Content(
            metrics.RenderPrometheus(),
            "text/plain; version=0.0.4; charset=utf-8");
}
