using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Itineraries.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItinerariesController(ItineraryPlannerService itineraryPlannerService) : ControllerBase
{
    [HttpPost("plan")]
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
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("plan/natural-language")]
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
            return BadRequest(new { message = ex.Message });
        }
    }
}
