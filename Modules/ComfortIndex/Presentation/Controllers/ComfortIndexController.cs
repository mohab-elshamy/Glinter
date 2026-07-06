using Glinter.Modules.ComfortIndex.Application.Abstractions;
using Glinter.Modules.ComfortIndex.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Glinter.Modules.ComfortIndex.Presentation.Controllers;

[ApiController]
[Route("api/comfort-index")]
[DisableRateLimiting]
public sealed class ComfortIndexController(
    IComfortIndexService comfortIndexService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? adm0Gid,
        [FromQuery] int? adm1Gid,
        [FromQuery] int? adm2Gid,
        [FromQuery] int? adm3Gid,
        CancellationToken cancellationToken)
        => Ok(await comfortIndexService.GetAsync(
            new ComfortIndexRequest(adm0Gid, adm1Gid, adm2Gid, adm3Gid),
            cancellationToken));

    [HttpPost("batch")]
    public async Task<IActionResult> GetBatch(
        [FromBody] ComfortIndexBatchRequest request,
        CancellationToken cancellationToken)
        => Ok(await comfortIndexService.GetBatchAsync(
            request?.Filters ?? [],
            cancellationToken));

    [HttpGet("adm0/{adm0Gid:int}")]
    public async Task<IActionResult> GetByAdm0(
        int adm0Gid,
        CancellationToken cancellationToken)
        => Ok(await comfortIndexService.GetAsync(
            new ComfortIndexRequest(Adm0Gid: adm0Gid),
            cancellationToken));

    [HttpGet("adm1/{adm1Gid:int}")]
    public async Task<IActionResult> GetByAdm1(
        int adm1Gid,
        CancellationToken cancellationToken)
        => Ok(await comfortIndexService.GetAsync(
            new ComfortIndexRequest(Adm1Gid: adm1Gid),
            cancellationToken));

    [HttpGet("adm2/{adm2Gid:int}")]
    public async Task<IActionResult> GetByAdm2(
        int adm2Gid,
        CancellationToken cancellationToken)
        => Ok(await comfortIndexService.GetAsync(
            new ComfortIndexRequest(Adm2Gid: adm2Gid),
            cancellationToken));

    [HttpGet("adm3/{adm3Gid:int}")]
    public async Task<IActionResult> GetByAdm3(
        int adm3Gid,
        CancellationToken cancellationToken)
        => Ok(await comfortIndexService.GetAsync(
            new ComfortIndexRequest(Adm3Gid: adm3Gid),
            cancellationToken));
}
