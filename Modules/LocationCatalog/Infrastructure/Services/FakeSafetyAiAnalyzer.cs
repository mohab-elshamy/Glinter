using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Services;

public sealed class FakeSafetyAiAnalyzer : ISafetyAiAnalyzer
{
    public Task<SafetyAiAnalysisResult> AnalyzeAsync(
        string districtName,
        string text,
        CancellationToken cancellationToken = default)
    {
        var normalizedText = text.ToLower();

        if (ContainsAny(normalizedText, "theft", "stolen", "robbery", "phone stolen", "pickpocket"))
        {
            return Task.FromResult(new SafetyAiAnalysisResult(
                RiskCategory: "theft",
                Severity: "medium",
                Confidence: 0.85,
                SentimentScore: -0.60,
                Summary: $"The text mentions theft-related concerns in {districtName}.",
                IsSafetyRelevant: true
            ));
        }

        if (ContainsAny(normalizedText, "harassment", "unsafe for women", "catcalling"))
        {
            return Task.FromResult(new SafetyAiAnalysisResult(
                RiskCategory: "harassment",
                Severity: "high",
                Confidence: 0.88,
                SentimentScore: -0.75,
                Summary: $"The text mentions harassment-related concerns in {districtName}.",
                IsSafetyRelevant: true
            ));
        }

        if (ContainsAny(normalizedText, "protest", "riot", "demonstration", "clashes"))
        {
            return Task.FromResult(new SafetyAiAnalysisResult(
                RiskCategory: "protest",
                Severity: "high",
                Confidence: 0.80,
                SentimentScore: -0.70,
                Summary: $"The text mentions protest or public disorder concerns in {districtName}.",
                IsSafetyRelevant: true
            ));
        }

        if (ContainsAny(normalizedText, "accident", "crash", "traffic accident", "road closed"))
        {
            return Task.FromResult(new SafetyAiAnalysisResult(
                RiskCategory: "accident",
                Severity: "low",
                Confidence: 0.75,
                SentimentScore: -0.35,
                Summary: $"The text mentions traffic or accident-related concerns in {districtName}.",
                IsSafetyRelevant: true
            ));
        }

        if (ContainsAny(normalizedText, "safe", "calm", "family friendly", "secure", "quiet"))
        {
            return Task.FromResult(new SafetyAiAnalysisResult(
                RiskCategory: "none",
                Severity: "none",
                Confidence: 0.70,
                SentimentScore: 0.45,
                Summary: $"The text contains positive safety signals about {districtName}.",
                IsSafetyRelevant: true
            ));
        }

        return Task.FromResult(new SafetyAiAnalysisResult(
            RiskCategory: "none",
            Severity: "none",
            Confidence: 0.60,
            SentimentScore: 0,
            Summary: "No clear safety-related signal was detected.",
            IsSafetyRelevant: false
        ));
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(text.Contains);
    }
}