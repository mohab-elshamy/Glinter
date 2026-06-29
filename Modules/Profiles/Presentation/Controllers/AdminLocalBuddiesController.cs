using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Profiles.Application.Profiles.Queries;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/admin/local-buddies")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
public class AdminLocalBuddiesController : ControllerBase
{
    private readonly UpdateLocalBuddyVerificationCommandHandler _updateLocalBuddyVerificationCommandHandler;
    private readonly GetLocalBuddyVerificationHistoryHandler _historyHandler;

    public AdminLocalBuddiesController(
        UpdateLocalBuddyVerificationCommandHandler updateLocalBuddyVerificationCommandHandler,
        GetLocalBuddyVerificationHistoryHandler historyHandler)
    {
        _updateLocalBuddyVerificationCommandHandler = updateLocalBuddyVerificationCommandHandler;
        _historyHandler = historyHandler;
    }

    [HttpPatch("{userId:guid}/verification")]
    public async Task<IActionResult> UpdateVerificationStatus(
        Guid userId,
        [FromBody] UpdateLocalBuddyVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateLocalBuddyVerificationCommandHandler.HandleAsync(
            new UpdateLocalBuddyVerificationCommand
            {
                UserId = userId,
                VerificationStatus = request.VerificationStatus,
                ModerationNotes = request.ModerationNotes
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{userId:guid}/verification-history")]
    public async Task<IActionResult> GetVerificationHistory(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _historyHandler.HandleAsync(
            userId,
            page,
            pageSize,
            cancellationToken));
}
