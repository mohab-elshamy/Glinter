using Glinter.Modules.Experiences.Application.Dtos;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceRecommendationService
{
    Task<ExperienceRecommendationResponse> RecommendAsync(
        ExperienceRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<NaturalLanguageExperienceRecommendationResponse> RecommendFromTextAsync(
        NaturalLanguageExperienceRecommendationRequest request,
        CancellationToken cancellationToken = default);
}
