using Glinter.Modules.Profiles.Application.Profiles.Queries.GetInterests;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/interests")]
public class InterestsController : ControllerBase
{
    private readonly GetInterestsQueryHandler _getInterestsQueryHandler;

    public InterestsController(GetInterestsQueryHandler getInterestsQueryHandler)
    {
        _getInterestsQueryHandler = getInterestsQueryHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetInterests(CancellationToken cancellationToken)
    {
        var result = await _getInterestsQueryHandler.HandleAsync(
            new GetInterestsQuery(),
            cancellationToken);

        return Ok(result);
    }
}