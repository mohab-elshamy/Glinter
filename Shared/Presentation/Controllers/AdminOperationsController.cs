using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Shared.Infrastructure.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Shared.Presentation.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminOperationsController : ControllerBase
{
    [HttpGet("/metrics")]
    [Produces("text/plain")]
    public ContentResult GetMetrics([FromServices] ApplicationMetrics metrics) =>
        Content(
            metrics.RenderPrometheus(),
            "text/plain; version=0.0.4; charset=utf-8");
}
