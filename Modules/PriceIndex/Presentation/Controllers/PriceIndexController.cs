using Glinter.Modules.PriceIndex.Application.Abstractions;
using Glinter.Modules.PriceIndex.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Glinter.Modules.PriceIndex.Presentation.Controllers;

[ApiController]
[Route("api/price-index")]
[DisableRateLimiting]
public sealed class PriceIndexController(
    IPriceIndexService priceIndexService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? adm0Gid,
        [FromQuery] int? adm1Gid,
        [FromQuery] int? adm2Gid,
        [FromQuery] int? adm3Gid,
        CancellationToken cancellationToken)
        => Ok(await priceIndexService.GetAsync(
            new PriceIndexRequest(adm0Gid, adm1Gid, adm2Gid, adm3Gid),
            cancellationToken));

    [HttpPost("batch")]
    public async Task<IActionResult> GetBatch(
        [FromBody] PriceIndexBatchRequest request,
        CancellationToken cancellationToken)
        => Ok(await priceIndexService.GetBatchAsync(
            request?.Filters ?? [],
            cancellationToken));

    [HttpGet("adm0/{adm0Gid:int}")]
    public async Task<IActionResult> GetByAdm0(
        int adm0Gid,
        CancellationToken cancellationToken)
        => Ok(await priceIndexService.GetAsync(
            new PriceIndexRequest(Adm0Gid: adm0Gid),
            cancellationToken));

    [HttpGet("adm1/{adm1Gid:int}")]
    public async Task<IActionResult> GetByAdm1(
        int adm1Gid,
        CancellationToken cancellationToken)
        => Ok(await priceIndexService.GetAsync(
            new PriceIndexRequest(Adm1Gid: adm1Gid),
            cancellationToken));

    [HttpGet("adm2/{adm2Gid:int}")]
    public async Task<IActionResult> GetByAdm2(
        int adm2Gid,
        CancellationToken cancellationToken)
        => Ok(await priceIndexService.GetAsync(
            new PriceIndexRequest(Adm2Gid: adm2Gid),
            cancellationToken));

    [HttpGet("adm3/{adm3Gid:int}")]
    public async Task<IActionResult> GetByAdm3(
        int adm3Gid,
        CancellationToken cancellationToken)
        => Ok(await priceIndexService.GetAsync(
            new PriceIndexRequest(Adm3Gid: adm3Gid),
            cancellationToken));
}
