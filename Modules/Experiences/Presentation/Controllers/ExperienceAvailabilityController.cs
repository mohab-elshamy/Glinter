using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/experience-availability")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = RoleNames.ExperienceProvider + "," + RoleNames.Admin)]
public class ExperienceAvailabilityController : ControllerBase
{
    private readonly ExperienceService _experienceService;

    public ExperienceAvailabilityController(ExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateExperienceAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.UpdateAvailabilityAsync(id, request, cancellationToken);
            return result is null ? NotFound(new { message = "Availability not found." }) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, true, cancellationToken);

    [HttpPatch("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, false, cancellationToken);

    private async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _experienceService.SetAvailabilityActiveAsync(id, isActive, cancellationToken);
            return result is null ? NotFound(new { message = "Availability not found." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
