using Glinter.Modules.Experiences.Application.Common.Mapping;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Commands.DeleteExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceReviews;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
public class ExperienceReviewsController : ControllerBase
{
    private readonly CreateExperienceReviewCommandHandler _createReviewHandler;
    private readonly UpdateExperienceReviewCommandHandler _updateReviewHandler;
    private readonly DeleteExperienceReviewCommandHandler _deleteReviewHandler;
    private readonly GetExperienceReviewsQueryHandler _getReviewsHandler;

    public ExperienceReviewsController(
        CreateExperienceReviewCommandHandler createReviewHandler,
        UpdateExperienceReviewCommandHandler updateReviewHandler,
        DeleteExperienceReviewCommandHandler deleteReviewHandler,
        GetExperienceReviewsQueryHandler getReviewsHandler)
    {
        _createReviewHandler = createReviewHandler;
        _updateReviewHandler = updateReviewHandler;
        _deleteReviewHandler = deleteReviewHandler;
        _getReviewsHandler = getReviewsHandler;
    }

    [HttpGet("api/experiences/{experienceId:guid}/reviews")]
    public async Task<ActionResult<List<ExperienceReviewResponseDto>>> GetByExperienceId(
        Guid experienceId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _getReviewsHandler.HandleAsync(
            new GetExperienceReviewsQuery
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

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPost("api/experiences/{experienceId:guid}/reviews")]
    public async Task<ActionResult<ExperienceReviewResponseDto>> Create(
        Guid experienceId,
        [FromBody] CreateExperienceReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _createReviewHandler.HandleAsync(
            request.ToCommand(experienceId),
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Experience was not found.");

        return CreatedAtAction(
            nameof(GetByExperienceId),
            new { experienceId = result.ExperienceId },
            result);
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPut("api/experience-reviews/{reviewId:guid}")]
    public async Task<ActionResult<ExperienceReviewResponseDto>> Update(
        Guid reviewId,
        [FromBody] UpdateExperienceReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _updateReviewHandler.HandleAsync(
            request.ToCommand(reviewId),
            cancellationToken);

        if (result is null)
            throw new NotFoundException("Review was not found.");

        return Ok(result);
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpDelete("api/experience-reviews/{reviewId:guid}")]
    public async Task<IActionResult> Delete(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        var deleted = await _deleteReviewHandler.HandleAsync(
            new DeleteExperienceReviewCommand
            {
                ReviewId = reviewId
            },
            cancellationToken);

        if (!deleted)
            throw new NotFoundException("Review was not found.");

        return NoContent();
    }
}
