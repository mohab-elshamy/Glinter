using Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceApprovalStatus;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetAdminExperiences;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/experiences")]
public class AdminExperiencesController : ControllerBase
{
    private readonly GetAdminExperiencesQueryHandler _getAdminExperiencesHandler;
    private readonly SetExperienceApprovalStatusCommandHandler _setApprovalStatusHandler;

    public AdminExperiencesController(
        GetAdminExperiencesQueryHandler getAdminExperiencesHandler,
        SetExperienceApprovalStatusCommandHandler setApprovalStatusHandler)
    {
        _getAdminExperiencesHandler = getAdminExperiencesHandler;
        _setApprovalStatusHandler = setApprovalStatusHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExperienceSummaryDto>>> GetAll(
        [FromQuery] GetAdminExperiencesRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            ExperienceApprovalStatus? approvalStatus = null;

            if (!string.IsNullOrWhiteSpace(request.ApprovalStatus))
            {
                if (!Enum.TryParse<ExperienceApprovalStatus>(
                        request.ApprovalStatus,
                        ignoreCase: true,
                        out var parsedStatus) ||
                    !Enum.IsDefined(parsedStatus))
                {
                    return BadRequest(new { message = "Invalid approval status." });
                }

                approvalStatus = parsedStatus;
            }

            var result = await _getAdminExperiencesHandler.HandleAsync(
                new GetAdminExperiencesQuery
                {
                    ApprovalStatus = approvalStatus,
                    IsActive = request.IsActive,
                    Page = request.Page,
                    PageSize = request.PageSize
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/approval-status")]
    public async Task<ActionResult<ExperienceResponseDto>> SetApprovalStatus(
        Guid id,
        [FromBody] SetExperienceApprovalStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Enum.TryParse<ExperienceApprovalStatus>(
                    request.ApprovalStatus,
                    ignoreCase: true,
                    out var approvalStatus) ||
                !Enum.IsDefined(approvalStatus))
            {
                return BadRequest(new { message = "Invalid approval status." });
            }

            var result = await _setApprovalStatusHandler.HandleAsync(
                new SetExperienceApprovalStatusCommand
                {
                    ExperienceId = id,
                    ApprovalStatus = approvalStatus,
                    ModerationNotes = request.ModerationNotes
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Experience was not found." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
