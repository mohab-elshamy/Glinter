using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Regions.Presentation.Controllers;

public static class RegionsUploadLimits
{
    public const long MaxGeoJsonUploadBytes = 50L * 1024 * 1024;
}

[ApiController]
[Route("api/admin/regions")]
[Authorize(Policy = PolicyNames.AdminOnly)]
[RequestSizeLimit(RegionsUploadLimits.MaxGeoJsonUploadBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = RegionsUploadLimits.MaxGeoJsonUploadBytes)]
public class AdminRegionsImportController(
    IGeoJsonImportService importService,
    LocalFileImportService localFileImportService
) : ControllerBase
{
    private const string AllowedExtension = ".geojson";

    // ─────────────────────────────────────────────────────────────
    //  File upload endpoints
    // ─────────────────────────────────────────────────────────────

    /// <summary>Import ADM0 (countries) from a GeoJSON file.</summary>
    [HttpPost("import/adm0")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportAdm0(IFormFile file, CancellationToken ct)
        => await ImportFile(file, importService.ImportAdm0Async, ct);

    /// <summary>Import ADM1 (governorates) from a GeoJSON file. Requires ADM0 data already imported.</summary>
    [HttpPost("import/adm1")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportAdm1(IFormFile file, CancellationToken ct)
        => await ImportFile(file, importService.ImportAdm1Async, ct);

    /// <summary>Import ADM2 (districts) from a GeoJSON file. Requires ADM1 data already imported.</summary>
    [HttpPost("import/adm2")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportAdm2(IFormFile file, CancellationToken ct)
        => await ImportFile(file, importService.ImportAdm2Async, ct);

    /// <summary>Import ADM3 (neighbourhoods) from a GeoJSON file. Requires ADM2 data already imported.</summary>
    [HttpPost("import/adm3")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportAdm3(IFormFile file, CancellationToken ct)
        => await ImportFile(file, importService.ImportAdm3Async, ct);

    // ─────────────────────────────────────────────────────────────
    //  Local file import (dev utility — all four levels in order)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Imports all four ADM levels from the local GeoJSON files bundled with the project.
    /// Order: adm0 → adm1 → adm2 → adm3.
    /// Stops early if any level fails.
    /// </summary>
    [HttpPost("import/all-local")]
    public async Task<IActionResult> ImportAllLocal(CancellationToken ct)
    {
        var results = await localFileImportService.ImportAllAsync(ct);
        var allSuccess = results.All(r => r.Success);
        return allSuccess ? Ok(results) : StatusCode(207, results); // 207 Multi-Status
    }

    // ─────────────────────────────────────────────────────────────
    //  Shared helper
    // ─────────────────────────────────────────────────────────────

    private async Task<IActionResult> ImportFile(
        IFormFile? file,
        Func<Stream, CancellationToken, Task<Application.DTOs.GeoJsonImportResultDto>> handler,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });

        if (file.Length > RegionsUploadLimits.MaxGeoJsonUploadBytes)
        {
            return BadRequest(new
            {
                message = "GeoJSON file is too large.",
                maxBytes = RegionsUploadLimits.MaxGeoJsonUploadBytes
            });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != AllowedExtension)
            return BadRequest(new { message = $"Only .geojson files are allowed. Got: '{ext}'" });

        await using var stream = file.OpenReadStream();
        var result = await handler(stream, ct);

        return result.Success ? Ok(result) : StatusCode(207, result);
    }
}
