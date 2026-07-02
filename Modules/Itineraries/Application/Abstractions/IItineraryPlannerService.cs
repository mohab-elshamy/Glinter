using Glinter.Modules.Itineraries.Application.Dtos;

namespace Glinter.Modules.Itineraries.Application.Abstractions;

public interface IItineraryPlannerService
{
    Task<ItineraryPlanResponse> PlanAsync(
        ItineraryPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<NaturalLanguageItineraryPlanResponse> PlanFromTextAsync(
        NaturalLanguageItineraryPlanRequest request,
        CancellationToken cancellationToken = default);
}
