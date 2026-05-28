using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetVibes;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Experiences.Presentation.Controllers;

[ApiController]
[Route("api/vibes")]
public class VibesController : ControllerBase
{
    private readonly GetVibesQueryHandler _getVibesHandler;

    public VibesController(GetVibesQueryHandler getVibesHandler)
    {
        _getVibesHandler = getVibesHandler;
    }

    [HttpGet]
    public async Task<ActionResult<List<VibeResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _getVibesHandler.HandleAsync(
            new GetVibesQuery(),
            cancellationToken);

        return Ok(result);
    }
}