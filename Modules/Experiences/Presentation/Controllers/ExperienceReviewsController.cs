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
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getReviewsHandler.HandleAsync(
                new GetExperienceReviewsQuery
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

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPost("api/experiences/{experienceId:guid}/reviews")]
    public async Task<ActionResult<ExperienceReviewResponseDto>> Create(
        Guid experienceId,
        [FromBody] CreateExperienceReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _createReviewHandler.HandleAsync(
                request.ToCommand(experienceId),
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
    }

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpPut("api/experience-reviews/{reviewId:guid}")]
    public async Task<ActionResult<ExperienceReviewResponseDto>> Update(
        Guid reviewId,
        [FromBody] UpdateExperienceReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _updateReviewHandler.HandleAsync(
                request.ToCommand(reviewId),
                cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = "Review was not found." });
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

    [Authorize(Roles = RoleNames.Traveler)]
    [HttpDelete("api/experience-reviews/{reviewId:guid}")]
    public async Task<IActionResult> Delete(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _deleteReviewHandler.HandleAsync(
                new DeleteExperienceReviewCommand
                {
                    ReviewId = reviewId
                },
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "Review was not found." });
            }

            return NoContent();
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