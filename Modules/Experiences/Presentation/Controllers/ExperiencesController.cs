using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperience;
using Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceActiveStatus;
using Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceById;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetAllExperiences;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/experiences")]
public class ExperiencesController : ControllerBase
{
    private readonly CreateExperienceCommandHandler _createExperienceHandler;
    private readonly UpdateExperienceCommandHandler _updateExperienceHandler;
    private readonly SetExperienceActiveStatusCommandHandler _setExperienceActiveStatusHandler;
    private readonly GetAllExperiencesQueryHandler _getAllExperiencesHandler;
    private readonly GetExperienceByIdQueryHandler _getExperienceByIdHandler;

    public ExperiencesController(
        CreateExperienceCommandHandler createExperienceHandler,
        UpdateExperienceCommandHandler updateExperienceHandler,
        SetExperienceActiveStatusCommandHandler setExperienceActiveStatusHandler,
        GetAllExperiencesQueryHandler getAllExperiencesHandler,
        GetExperienceByIdQueryHandler getExperienceByIdHandler)
    {
        _createExperienceHandler = createExperienceHandler;
        _updateExperienceHandler = updateExperienceHandler;
        _setExperienceActiveStatusHandler = setExperienceActiveStatusHandler;
        _getAllExperiencesHandler = getAllExperiencesHandler;
        _getExperienceByIdHandler = getExperienceByIdHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExperienceSummaryDto>>> GetAll(
        [FromQuery] GetExperiencesRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getAllExperiencesHandler.HandleAsync(
                request.ToQuery(),
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExperienceResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getExperienceByIdHandler.HandleAsync(
                new GetExperienceByIdQuery
                {
                    Id = id
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
    [HttpPost]
    public async Task<ActionResult<ExperienceResponseDto>> Create(
        [FromBody] CreateExperienceRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _createExperienceHandler.HandleAsync(
                request.ToCommand(),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
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
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExperienceResponseDto>> Update(
        Guid id,
        [FromBody] UpdateExperienceRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _updateExperienceHandler.HandleAsync(
                request.ToCommand(id),
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
    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<ExperienceResponseDto>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _setExperienceActiveStatusHandler.HandleAsync(
                new SetExperienceActiveStatusCommand
                {
                    ExperienceId = id,
                    IsActive = true
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<ExperienceResponseDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _setExperienceActiveStatusHandler.HandleAsync(
                new SetExperienceActiveStatusCommand
                {
                    ExperienceId = id,
                    IsActive = false
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}