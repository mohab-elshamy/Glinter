using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.SafetyIndex.Presentation.Controllers;

[ApiController]
[Route("api/safety-index")]
public class SafetyIndexController(ISafetyIndexService safetyIndexService) : ControllerBase
{
    [HttpGet("adm2/{adm2Gid:int}")]
    public async Task<IActionResult> GetByAdm2(int adm2Gid, CancellationToken ct)
    {
        var result = await safetyIndexService.GetByAdm2Async(adm2Gid, ct);
        return result is null ? NotFound(new { message = "ADM2 area not found." }) : Ok(result);
    }

    [HttpGet("adm1/{adm1Gid:int}")]
    public async Task<IActionResult> GetByAdm1(int adm1Gid, CancellationToken ct)
        => Ok(await safetyIndexService.GetByAdm1Async(adm1Gid, ct));

    [HttpGet("adm0/{adm0Gid:int}")]
    public async Task<IActionResult> GetByAdm0(int adm0Gid, CancellationToken ct)
        => Ok(await safetyIndexService.GetByAdm0Async(adm0Gid, ct));
}
