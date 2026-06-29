using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Shared.Application.Auditing;
using Glinter.Shared.Infrastructure.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Shared.Presentation.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/audit-events")]
public sealed class AdminAuditController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminAuditPageDto>> Get(
        [FromServices] AdminAuditService auditService,
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await auditService.GetAsync(
            actorUserId,
            action,
            fromUtc,
            toUtc,
            page,
            pageSize,
            cancellationToken));
}
