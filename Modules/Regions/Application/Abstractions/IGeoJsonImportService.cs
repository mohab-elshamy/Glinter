using Glinter.Modules.Regions.Application.DTOs;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IGeoJsonImportService
{
    Task<GeoJsonImportResultDto> ImportAdm0Async(Stream geojsonStream, CancellationToken ct = default);
    Task<GeoJsonImportResultDto> ImportAdm1Async(Stream geojsonStream, CancellationToken ct = default);
    Task<GeoJsonImportResultDto> ImportAdm2Async(Stream geojsonStream, CancellationToken ct = default);
    Task<GeoJsonImportResultDto> ImportAdm3Async(Stream geojsonStream, CancellationToken ct = default);
}
