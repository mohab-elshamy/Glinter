namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record ImportDistrictNewsResponse(
    Guid DistrictId,
    string DistrictNameEn,
    string Query,
    int FetchedArticles,
    int AnalyzedArticles,
    int SkippedArticles,
    int SafetyRelevantArticles,
    double SafetyScore,
    string SafetyLevel,
    string Explanation,
    List<string> FetchedArticleTitles,
    List<string> AnalyzedArticleTitles
);