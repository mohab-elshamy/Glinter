using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Services;
using Glinter.Modules.Itineraries.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Itineraries.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItinerariesController(
    IItineraryPlannerService itineraryPlannerService,
    SavedItineraryService savedItineraryService) : ControllerBase
{
    [HttpPost("plan")]
    [EnableRateLimiting(ItineraryPlanningRateLimitPolicies.AiRequests)]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Plan(
        [FromBody] ItineraryPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await itineraryPlannerService.PlanAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return PlanningValidationProblem(ex);
        }
    }

    [HttpPost("plan/natural-language")]
    [EnableRateLimiting(ItineraryPlanningRateLimitPolicies.AiRequests)]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> PlanFromNaturalLanguage(
        [FromBody] NaturalLanguageItineraryPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await itineraryPlannerService.PlanFromTextAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return PlanningValidationProblem(ex);
        }
    }

    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = RoleNames.Traveler)]
    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] SaveItineraryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await savedItineraryService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSaved), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return PlanningValidationProblem(ex);
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet]
    public async Task<IActionResult> ListSaved(CancellationToken cancellationToken) =>
        Ok(await savedItineraryService.ListAsync(cancellationToken));

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSaved(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await savedItineraryService.GetAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSaved(
        Guid id,
        [FromBody] UpdateSavedItineraryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await savedItineraryService.UpdateAsync(id, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return PlanningValidationProblem(ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("{id:guid}/items")]
    public async Task<IActionResult> ReplaceItems(
        Guid id,
        [FromBody] ReplaceItineraryItemsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await savedItineraryService.ReplaceItemsAsync(
                id,
                request,
                cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return PlanningValidationProblem(ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return ConflictProblem(ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSaved(
        Guid id,
        CancellationToken cancellationToken) =>
        await savedItineraryService.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();

    private IActionResult PlanningValidationProblem(ArgumentException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid itinerary request.",
            Detail = exception.Message,
            Instance = Request.Path
        };
        problem.Extensions["errorCode"] = "invalid_itinerary_request";
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return BadRequest(problem);
    }

    private IActionResult ConflictProblem(string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Itinerary update conflict.",
            Detail = detail,
            Instance = Request.Path
        };
        problem.Extensions["errorCode"] = "itinerary_update_conflict";
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return Conflict(problem);
    }
}
