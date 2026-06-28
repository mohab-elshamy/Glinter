using Glinter.Modules.SafetyIndex.Application.Dtos;

namespace Glinter.Modules.SafetyIndex.Application.Abstractions;

public interface ISafetyScoringClient
{
    Task<SafetyScoreEstimateDto?> EstimateAsync(
        string areaNameAr,
        IReadOnlyCollection<string> titles,
        SafetyScorePeriod period,
        CancellationToken ct = default);
}
