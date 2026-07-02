using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Itineraries.Presentation.Controllers;

[ApiController]
[Route("api/weather")]
public sealed class WeatherController(
    IWeatherForecastService weatherForecastService) : ControllerBase
{
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(
        [FromQuery] WeatherForecastRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await weatherForecastService.GetForecastAsync(
                request,
                cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid weather request.",
                Detail = ex.Message,
                Instance = Request.Path
            });
        }
    }
}
