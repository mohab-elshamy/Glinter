namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record DistrictSafetySignalResponse(
    Guid Id,
    Guid DistrictId,
    string SourceType,
    string Title,
    string RiskCategory,
    string Severity,
    double Confidence,
    double SentimentScore,
    bool IsSafetyRelevant,
    string AiSummary,
    DateTime PublishedAtUtc,
    DateTime AnalyzedAtUtc
);