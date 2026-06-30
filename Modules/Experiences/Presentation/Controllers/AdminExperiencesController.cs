using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Shared.Application.Auditing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/admin/experiences")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleNames.Admin)]
public class AdminExperiencesController : ControllerBase
{
    private readonly ExperienceService _experienceService;
    private readonly AdminAuditDetailsContext _auditDetails;

    public AdminExperiencesController(
        ExperienceService experienceService,
        AdminAuditDetailsContext auditDetails)
    {
        _experienceService = experienceService;
        _auditDetails = auditDetails;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] ExperienceModerationStatus? moderationStatus,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _experienceService.GetAdminExperiencesAsync(
            moderationStatus,
            isActive,
            cancellationToken));
    }

    [HttpPatch("{id:int}/moderation")]
    public async Task<IActionResult> Moderate(
        int id,
        [FromBody] ModerateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.ModerateAsync(id, request, cancellationToken);
            if (result is null)
            {
                return NotFound(new { message = "Experience not found." });
            }

            _auditDetails.SetChanges(
                new { },
                new
                {
                    result.ModerationStatus,
                    result.ModerationNotes,
                    result.IsActive
                });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
