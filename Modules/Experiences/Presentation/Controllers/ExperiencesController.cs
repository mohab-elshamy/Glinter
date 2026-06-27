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
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetMyExperiences;

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
    private readonly GetMyExperiencesQueryHandler _getMyExperiencesHandler;

    public ExperiencesController(
        CreateExperienceCommandHandler createExperienceHandler,
        UpdateExperienceCommandHandler updateExperienceHandler,
        SetExperienceActiveStatusCommandHandler setExperienceActiveStatusHandler,
        GetAllExperiencesQueryHandler getAllExperiencesHandler,
        GetExperienceByIdQueryHandler getExperienceByIdHandler,
        GetMyExperiencesQueryHandler getMyExperiencesHandler)
    {
        _createExperienceHandler = createExperienceHandler;
        _updateExperienceHandler = updateExperienceHandler;
        _setExperienceActiveStatusHandler = setExperienceActiveStatusHandler;
        _getAllExperiencesHandler = getAllExperiencesHandler;
        _getExperienceByIdHandler = getExperienceByIdHandler;
        _getMyExperiencesHandler = getMyExperiencesHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExperienceSummaryDto>>> GetAll(
        [FromQuery] GetExperiencesRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _getAllExperiencesHandler.HandleAsync(
            request.ToQuery(),
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpGet("my")]
    public async Task<ActionResult<List<ExperienceSummaryDto>>> GetMyExperiences(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _getMyExperiencesHandler.HandleAsync(
            new GetMyExperiencesQuery
            {
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExperienceResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getExperienceByIdHandler.HandleAsync(
            new GetExperienceByIdQuery
            {
                Id = id
            },
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPost]
    public async Task<ActionResult<ExperienceResponseDto>> Create(
        [FromBody] CreateExperienceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _createExperienceHandler.HandleAsync(
            request.ToCommand(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExperienceResponseDto>> Update(
        Guid id,
        [FromBody] UpdateExperienceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _updateExperienceHandler.HandleAsync(
            request.ToCommand(id),
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<ExperienceResponseDto>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _setExperienceActiveStatusHandler.HandleAsync(
            new SetExperienceActiveStatusCommand
            {
                ExperienceId = id,
                IsActive = true
            },
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<ExperienceResponseDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _setExperienceActiveStatusHandler.HandleAsync(
            new SetExperienceActiveStatusCommand
            {
                ExperienceId = id,
                IsActive = false
            },
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return Ok(result);
    }
}
