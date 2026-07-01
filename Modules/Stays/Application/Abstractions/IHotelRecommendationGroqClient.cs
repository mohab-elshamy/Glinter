using Glinter.Modules.Stays.Application.Dtos;

namespace Glinter.Modules.Stays.Application.Abstractions;

public interface IHotelRecommendationGroqClient
{
    Task<HotelRecommendationPreferences?> ClassifyAsync(
        string text,
        string? preferredLanguage,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, HotelRecommendationExplanationResponse>?> GenerateExplanationsAsync(
        HotelRecommendationRequest request,
        IReadOnlyList<HotelRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken);
}
