namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record NewsArticleDto(
    string Title,
    string? Description,
    string? Content,
    string? Url,
    DateTime? PublishedAtUtc,
    string SourceName
);