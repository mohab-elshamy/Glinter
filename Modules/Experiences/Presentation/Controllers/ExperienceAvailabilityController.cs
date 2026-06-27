using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Commands.ActivateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.DeactivateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
public class ExperienceAvailabilityController : ControllerBase
{
    private readonly CreateExperienceAvailabilityCommandHandler _createAvailabilityHandler;
    private readonly DeactivateExperienceAvailabilityCommandHandler _deactivateAvailabilityHandler;
    private readonly ActivateExperienceAvailabilityCommandHandler _activateAvailabilityHandler;
    private readonly GetExperienceAvailabilityQueryHandler _getAvailabilityHandler;

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
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _getAvailabilityHandler.HandleAsync(
            new GetExperienceAvailabilityQuery
            {
                ExperienceId = experienceId,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPost("api/experiences/{experienceId:guid}/availability")]
    public async Task<ActionResult<ExperienceAvailabilityResponseDto>> Create(
        Guid experienceId,
        [FromBody] CreateExperienceAvailabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _createAvailabilityHandler.HandleAsync(
            request.ToCommand(experienceId),
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return CreatedAtAction(
            nameof(GetByExperienceId),
            new { experienceId = result.ExperienceId },
            result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("api/experience-availability/{availabilityId:guid}/activate")]
    public async Task<ActionResult<ExperienceAvailabilityResponseDto>> Activate(
        Guid availabilityId,
        CancellationToken cancellationToken)
    {
        var result = await _activateAvailabilityHandler.HandleAsync(
            new ActivateExperienceAvailabilityCommand
            {
                AvailabilityId = availabilityId
            },
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Availability slot was not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.ExperienceProvider)]
    [HttpPatch("api/experience-availability/{availabilityId:guid}/deactivate")]
    public async Task<ActionResult<ExperienceAvailabilityResponseDto>> Deactivate(
        Guid availabilityId,
        CancellationToken cancellationToken)
    {
        var result = await _deactivateAvailabilityHandler.HandleAsync(
            new DeactivateExperienceAvailabilityCommand
            {
                AvailabilityId = availabilityId
            },
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Availability slot was not found.");

        return Ok(result);
    }
}
