namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceRecommendationReadService
{
    Task<IReadOnlyList<ExperienceRecommendationCandidate>> GetCandidatesAsync(
        ExperienceRecommendationQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record ExperienceRecommendationQuery(
    IReadOnlyCollection<string> CategoryNames,
    int? Adm0Gid,
    int? Adm1Gid,
    int? Adm2Gid,
    int? Adm3Gid,
    int MaxPerCategory);

public sealed record ExperienceRecommendationCandidate(
    int Id,
    string Category,
    string Name,
    double Latitude,
    double Longitude,
    decimal? Rating,
    int? Reviews);
