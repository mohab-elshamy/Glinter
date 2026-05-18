namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record SafetyAiAnalysisResult(
    string RiskCategory,
    string Severity,
    double Confidence,
    double SentimentScore,
    string Summary,
    bool IsSafetyRelevant
);