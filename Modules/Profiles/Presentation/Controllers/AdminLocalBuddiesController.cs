using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Profiles.Application.Profiles.Queries;
using Glinter.Modules.Profiles.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Glinter.Modules.Profiles.Domain.Enums;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/admin/local-buddies")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
public class AdminLocalBuddiesController : ControllerBase
{
    private readonly UpdateLocalBuddyVerificationCommandHandler _updateLocalBuddyVerificationCommandHandler;
    private readonly GetLocalBuddyVerificationHistoryHandler _historyHandler;
    private readonly IProfilesDbContext _dbContext;

    public AdminLocalBuddiesController(
        UpdateLocalBuddyVerificationCommandHandler updateLocalBuddyVerificationCommandHandler,
        GetLocalBuddyVerificationHistoryHandler historyHandler,
        IProfilesDbContext dbContext)
    {
        _updateLocalBuddyVerificationCommandHandler = updateLocalBuddyVerificationCommandHandler;
        _historyHandler = historyHandler;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? verificationStatus,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.LocalBuddyProfiles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(verificationStatus))
        {
            if (!Enum.TryParse<VerificationStatus>(
                    verificationStatus,
                    true,
                    out var parsedStatus))
            {
                return BadRequest(new { message = "Invalid verification status." });
            }
            query = query.Where(x => x.VerificationStatus == parsedStatus);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminLocalBuddyListItemResponse
            {
                UserId = x.UserId,
                ProfileId = x.Id,
                DisplayName = x.DisplayName,
                City = x.City,
                Languages = x.Languages,
                ProfileImageUrl = x.ProfileImageUrl,
                Rating = x.Rating,
                ReviewsCount = x.ReviewsCount,
                VerificationStatus = x.VerificationStatus.ToString(),
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
        return Ok(items);
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
