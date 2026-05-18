using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Safety.Services;

public sealed class DistrictSafetyScoreService
{
    private readonly ILocationCatalogDbContext _dbContext;

    public DistrictSafetyScoreService(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DistrictSafetyScoreResponse> CalculateAsync(
        Guid districtId,
        CancellationToken cancellationToken = default)
    {
        return await CalculateCoreAsync(districtId, cancellationToken);
    }

    public async Task<DistrictSafetyScoreResponse> CalculateAndSaveAsync(
        Guid districtId,
        CancellationToken cancellationToken = default)
    {
        var result = await CalculateCoreAsync(districtId, cancellationToken);

        var districtIndex = await _dbContext.DistrictIndices
            .FirstOrDefaultAsync(x => x.DistrictId == districtId, cancellationToken);

        if (districtIndex is null)
        {
            districtIndex = new DistrictIndex
            {
                Id = Guid.NewGuid(),
                DistrictId = districtId
            };

            _dbContext.DistrictIndices.Add(districtIndex);
        }

        districtIndex.SafetyScore = result.SafetyScore;
        districtIndex.SafetyLevel = result.SafetyLevel;
        districtIndex.SafetyExplanation = result.Explanation;
        districtIndex.ComputedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<DistrictSafetyScoreResponse> CalculateCoreAsync(
        Guid districtId,
        CancellationToken cancellationToken = default)
    {
        var district = await _dbContext.Districts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == districtId, cancellationToken);

        if (district is null)
        {
            throw new InvalidOperationException("District was not found.");
        }

        var signals = await _dbContext.DistrictSafetySignals
            .AsNoTracking()
            .Where(x => x.DistrictId == districtId)
            .ToListAsync(cancellationToken);

        var relevantSignals = signals
            .Where(x => x.IsSafetyRelevant)
            .ToList();

        var score = 85.0;

        foreach (var signal in relevantSignals)
        {
            var penalty = GetSeverityPenalty(signal.Severity);
            var recencyMultiplier = GetRecencyMultiplier(signal.PublishedAtUtc);

            if (signal.SentimentScore > 0 &&
                signal.Severity.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                score += 2 * signal.Confidence * recencyMultiplier;
                continue;
            }

            score -= penalty * signal.Confidence * recencyMultiplier;
        }

        score = ClampScore(score);

        var safetyLevel = GetSafetyLevel(score);

        var explanation = BuildExplanation(
            safetyLevel,
            relevantSignals.Count,
            relevantSignals.Select(x => x.RiskCategory).Distinct().ToList());

        return new DistrictSafetyScoreResponse(
            district.Id,
            district.NameEn,
            district.NameAr,
            Math.Round(score, 2),
            safetyLevel,
            signals.Count,
            relevantSignals.Count,
            explanation
        );
    }

    private static double GetSeverityPenalty(string severity)
    {
        return severity.ToLower() switch
        {
            "low" => 2,
            "medium" => 5,
            "high" => 10,
            "critical" => 20,
            _ => 0
        };
    }

    private static double GetRecencyMultiplier(DateTime publishedAtUtc)
    {
        var daysOld = (DateTime.UtcNow - publishedAtUtc).TotalDays;

        if (daysOld <= 7)
        {
            return 1.0;
        }

        if (daysOld <= 30)
        {
            return 0.7;
        }

        if (daysOld <= 90)
        {
            return 0.4;
        }

        return 0.2;
    }

    private static string GetSafetyLevel(double score)
    {
        return score switch
        {
            >= 80 => "High",
            >= 60 => "Medium",
            >= 40 => "Low",
            _ => "Very Low"
        };
    }

    private static double ClampScore(double score)
    {
        return Math.Max(0, Math.Min(100, score));
    }

    private static string BuildExplanation(
        string safetyLevel,
        int relevantSignalsCount,
        List<string> riskCategories)
    {
        if (relevantSignalsCount == 0)
        {
            return $"Safety level is {safetyLevel}. No relevant safety concerns were detected for this district.";
        }

        var categories = riskCategories
            .Where(x => !string.IsNullOrWhiteSpace(x) &&
                        !x.Equals("none", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (categories.Count == 0)
        {
            return $"Safety level is {safetyLevel}. Recent signals are mostly neutral or positive.";
        }

        return $"Safety level is {safetyLevel}. Recent signals mention: {string.Join(", ", categories)}.";
    }
}