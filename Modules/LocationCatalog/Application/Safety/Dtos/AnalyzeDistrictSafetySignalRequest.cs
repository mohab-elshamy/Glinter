namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record AnalyzeDistrictSafetySignalRequest(
    string SourceType,
    string Title,
    string Content,
    string? SourceUrl,
    DateTime? PublishedAtUtc
);