using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;

namespace Glinter.Modules.Regions.Infrastructure.Services;

/// <summary>
/// Imports all four ADM levels from the local GeoJSON files shipped with the project.
/// Import order: adm0 → adm1 → adm2 → adm3 (parent must exist before child).
/// </summary>
public class LocalFileImportService(IGeoJsonImportService importService, ILogger<LocalFileImportService> logger)
{
    private static readonly string BasePath =
        Path.Combine(Directory.GetCurrentDirectory(), "egy_admin_boundaries.geojson");

    public async Task<List<GeoJsonImportResultDto>> ImportAllAsync(CancellationToken ct = default)
    {
        var results = new List<GeoJsonImportResultDto>();

        string[] files =
        [
            Path.Combine(BasePath, "egy_admin0.geojson"),
            Path.Combine(BasePath, "egy_admin1.geojson"),
            Path.Combine(BasePath, "egy_admin2.geojson"),
            Path.Combine(BasePath, "egy_admin3.geojson"),
        ];

        Func<Stream, CancellationToken, Task<GeoJsonImportResultDto>>[] importers =
        [
            importService.ImportAdm0Async,
            importService.ImportAdm1Async,
            importService.ImportAdm2Async,
            importService.ImportAdm3Async,
        ];

        for (var i = 0; i < files.Length; i++)
        {
            var filePath = files[i];
            logger.LogInformation("Importing {FilePath}...", filePath);

            if (!File.Exists(filePath))
            {
                logger.LogWarning("File not found: {FilePath}", filePath);
                results.Add(new GeoJsonImportResultDto
                {
                    Layer = $"adm{i}",
                    Success = false,
                    Errors = [$"File not found: {filePath}"],
                });
                break; // Cannot continue without parent data
            }

            await using var stream = File.OpenRead(filePath);
            var result = await importers[i](stream, ct);
            results.Add(result);

            logger.LogInformation(
                "adm{Level}: total={Total} inserted={Inserted} updated={Updated} skipped={Skipped}",
                i, result.TotalFeatures, result.Inserted, result.Updated, result.Skipped);

            if (!result.Success)
            {
                logger.LogError("Import failed at adm{Level}, stopping.", i);
                break;
            }
        }

        return results;
    }
}
