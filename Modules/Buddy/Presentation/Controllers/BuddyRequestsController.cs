using Glinter.Modules.Buddy.Application.Requests;
using Glinter.Modules.Buddy.Application.Requests.Dtos;
using Glinter.Modules.Buddy.Application.Reviews.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Buddy.Presentation.Controllers;

[ApiController]
[Route("api/buddy/requests")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class BuddyRequestsController(BuddyRequestService service)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBuddyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateRequestAsync(
            request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { requestId = result.Id }, result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> GetMine(
        CancellationToken cancellationToken) =>
        Ok(await service.GetMineAsync(cancellationToken));

    [HttpGet("incoming")]
    [Authorize(Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> GetIncoming(
        CancellationToken cancellationToken) =>
        Ok(await service.GetIncomingAsync(cancellationToken));

    [HttpGet("{requestId:guid}")]
    public async Task<IActionResult> GetById(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await service.GetRequestAsync(requestId, cancellationToken));

    [HttpPatch("{requestId:guid}/accept")]
    [Authorize(Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> Accept(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await service.AcceptAsync(requestId, cancellationToken));

    [HttpPatch("{requestId:guid}/reject")]
    [Authorize(Roles = RoleNames.LocalBuddy)]
    public async Task<IActionResult> Reject(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await service.RejectAsync(requestId, cancellationToken));

    [HttpPatch("{requestId:guid}/cancel")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> Cancel(
        Guid requestId,
        CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(requestId, cancellationToken));

    [HttpPost("{requestId:guid}/review")]
    [Authorize(Roles = RoleNames.Traveler)]
    public async Task<IActionResult> Review(
        Guid requestId,
        [FromBody] CreateBuddyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateReviewAsync(
            requestId, request, cancellationToken);
        return Created(
            $"/api/local-buddies/{result.LocalBuddyUserId}/reviews/{result.Id}",
            result);
    }
}
