using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.DeactivateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceAvailability;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Experiences.Application.Experiences.Commands.ActivateExperienceAvailability;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
public class ExperienceAvailabilityController : ControllerBase
{
    private readonly CreateExperienceAvailabilityCommandHandler _createAvailabilityHandler;
    private readonly DeactivateExperienceAvailabilityCommandHandler _deactivateAvailabilityHandler;
    private readonly GetExperienceAvailabilityQueryHandler _getAvailabilityHandler;
    private readonly ActivateExperienceAvailabilityCommandHandler _activateAvailabilityHandler;
    
    public ExperienceAvailabilityController(
        CreateExperienceAvailabilityCommandHandler createAvailabilityHandler,
        DeactivateExperienceAvailabilityCommandHandler deactivateAvailabilityHandler,
        ActivateExperienceAvailabilityCommandHandler activateAvailabilityHandler,
        GetExperienceAvailabilityQueryHandler getAvailabilityHandler)
    {
        _createAvailabilityHandler = createAvailabilityHandler;
        _deactivateAvailabilityHandler = deactivateAvailabilityHandler;
        _activateAvailabilityHandler = activateAvailabilityHandler;
        _getAvailabilityHandler = getAvailabilityHandler;
    }

    [HttpGet("api/experiences/{experienceId:guid}/availability")]
    public async Task<ActionResult<List<ExperienceAvailabilityResponseDto>>> GetByExperienceId(
        Guid experienceId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getAvailabilityHandler.HandleAsync(
                new GetExperienceAvailabilityQuery
                {
                    ExperienceId = experienceId
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

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPost("api/experiences/{experienceId:guid}/availability")]
    public async Task<ActionResult<ExperienceAvailabilityResponseDto>> Create(
        Guid experienceId,
        [FromBody] CreateExperienceAvailabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _createAvailabilityHandler.HandleAsync(
                new CreateExperienceAvailabilityCommand
                {
                    ExperienceId = experienceId,
                    StartTimeUtc = request.StartTimeUtc,
                    EndTimeUtc = request.EndTimeUtc,
                    Capacity = request.Capacity
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Experience was not found." });
            }

            return CreatedAtAction(
                nameof(GetByExperienceId),
                new { experienceId = result.ExperienceId },
                result);
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

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("api/experience-availability/{availabilityId:guid}/deactivate")]
    public async Task<ActionResult<ExperienceAvailabilityResponseDto>> Deactivate(
        Guid availabilityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _deactivateAvailabilityHandler.HandleAsync(
                new DeactivateExperienceAvailabilityCommand
                {
                    AvailabilityId = availabilityId
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Availability slot was not found." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
    
    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("api/experience-availability/{availabilityId:guid}/activate")]
    public async Task<ActionResult<ExperienceAvailabilityResponseDto>> Activate(
        Guid availabilityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _activateAvailabilityHandler.HandleAsync(
                new ActivateExperienceAvailabilityCommand
                {
                    AvailabilityId = availabilityId
                },
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Availability slot was not found." });
            }

            return Ok(result);
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
}