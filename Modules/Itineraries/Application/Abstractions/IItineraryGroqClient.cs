using Glinter.Modules.Itineraries.Application.Dtos;

namespace Glinter.Modules.Itineraries.Application.Abstractions;

public interface IItineraryGroqClient
{
    Task<ItineraryPlanPreferences?> ClassifyPlanAsync(
        string text,
        string? preferredLanguage,
        CancellationToken cancellationToken);
}
