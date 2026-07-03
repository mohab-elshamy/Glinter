using Glinter.Modules.Experiences.Application.Dtos;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceRecommendationGroqClient
{
    Task<ExperienceRecommendationPreferences?> ClassifyAsync(
        string text,
        string? preferredLanguage,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, ExperienceRecommendationExplanationResponse>?> GenerateExplanationsAsync(
        ExperienceRecommendationPreferences preferences,
        IReadOnlyList<ExperienceRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken);
}
