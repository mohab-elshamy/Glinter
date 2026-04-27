using Glinter.Modules.Stays.Application.Reviews.Commands;
using Glinter.Modules.Stays.Application.Reviews.Dtos;
using Glinter.Modules.Stays.Application.Reviews.Queries;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.Stays.Application.Reviews.Commands;
using Glinter.Modules.Stays.Application.Reviews.Dtos;
namespace Glinter.Modules.Stays.Presentation.Controllers;

[ApiController]
[Route("api/stays/{stayId:guid}/reviews")]
public class StayReviewController : ControllerBase
{
    private readonly DeleteStayReviewHandler _deleteStayReviewHandler;
    private readonly CreateStayReviewHandler _createStayReviewHandler;
    private readonly GetStayReviewsHandler _getStayReviewsHandler;
    private readonly UpdateStayReviewHandler _updateStayReviewHandler;

    public StayReviewController(
        CreateStayReviewHandler createStayReviewHandler,
        GetStayReviewsHandler getStayReviewsHandler,
        UpdateStayReviewHandler updateStayReviewHandler,
        DeleteStayReviewHandler deleteStayReviewHandler)
    {
        _createStayReviewHandler = createStayReviewHandler;
        _getStayReviewsHandler = getStayReviewsHandler;
        _updateStayReviewHandler = updateStayReviewHandler;
        _deleteStayReviewHandler = deleteStayReviewHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview(
        Guid stayId,
        [FromBody] CreateStayReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateStayReviewCommand
            {
                StayId = stayId,
                TravelerProfileId = request.TravelerProfileId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            var result = await _createStayReviewHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid stayId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetStayReviewsQuery
            {
                StayId = stayId
            };

            var result = await _getStayReviewsHandler.HandleAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    
    [HttpPut("~/api/stay-reviews/{reviewId:guid}")]
    public async Task<IActionResult> UpdateReview(
        Guid reviewId,
        [FromBody] UpdateStayReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateStayReviewCommand
            {
                ReviewId = reviewId,
                TravelerProfileId = request.TravelerProfileId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            var result = await _updateStayReviewHandler.HandleAsync(command, cancellationToken);

            if (result is null)
                return NotFound(new { message = "Review not found." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    [HttpDelete("~/api/stay-reviews/{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        [FromBody] DeleteStayReviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteStayReviewCommand
            {
                ReviewId = reviewId,
                TravelerProfileId = request.TravelerProfileId
            };

            var deleted = await _deleteStayReviewHandler.HandleAsync(command, cancellationToken);

            if (!deleted)
                return NotFound(new { message = "Review not found." });

            return Ok(new { message = "Review deleted successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    public class DeleteStayReviewRequest
    {
        public Guid TravelerProfileId { get; set; }
    }
}