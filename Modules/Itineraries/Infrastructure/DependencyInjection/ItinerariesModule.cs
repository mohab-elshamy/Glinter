using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Application.Services;
using Glinter.Modules.Itineraries.Infrastructure.External.Groq;
using Glinter.Modules.Itineraries.Infrastructure.Routing;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.DependencyInjection;

public static class ItinerariesModule
{
    public static IServiceCollection AddItinerariesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ItineraryPlanningOptions>(
            configuration.GetSection(ItineraryPlanningOptions.SectionName));
        services.PostConfigure<ItineraryPlanningOptions>(options =>
        {
            options.OpenRouteServiceApiKey = FirstConfigured(
                options.OpenRouteServiceApiKey,
                configuration["OPENROUTESERVICE_API_KEY"]);
            options.OpenRouteServiceBaseUrl = FirstConfigured(
                options.OpenRouteServiceBaseUrl,
                configuration["OPENROUTESERVICE_BASE_URL"],
                "https://api.openrouteservice.org");
            options.OpenTripPlannerBaseUrl = FirstConfigured(
                options.OpenTripPlannerBaseUrl,
                configuration["OPENTRIPPLANNER_BASE_URL"],
                "http://localhost:8040");
            options.GroqApiKey = FirstConfigured(
                options.GroqApiKey,
                configuration["GROQ_API_KEY"],
                configuration["ExperienceRecommendations:GroqApiKey"],
                configuration["HotelRecommendations:GroqApiKey"],
                configuration["SafetyIndex:GroqApiKey"]);
            options.GroqModel = FirstConfigured(
                options.GroqModel,
                configuration["GROQ_MODEL"],
                configuration["ExperienceRecommendations:GroqModel"],
                configuration["HotelRecommendations:GroqModel"],
                configuration["SafetyIndex:GroqModel"],
                "llama-3.1-8b-instant");
            options.GroqBaseUrl = FirstConfigured(
                options.GroqBaseUrl,
                configuration["GROQ_BASE_URL"],
                configuration["ExperienceRecommendations:GroqBaseUrl"],
                configuration["HotelRecommendations:GroqBaseUrl"],
                configuration["SafetyIndex:GroqChatCompletionsUrl"],
                "https://api.groq.com/openai/v1");
        });

        services.AddScoped<ItineraryPlannerService>();
        services.AddScoped<IItineraryRoutePlanner, ItineraryRoutePlanner>();

        services.AddHttpClient<OpenRouteServiceClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<ItineraryPlanningOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.RoutingRequestTimeoutSeconds));
        });

        services.AddHttpClient<OpenTripPlannerClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<ItineraryPlanningOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.RoutingRequestTimeoutSeconds));
        });

        services.AddHttpClient<IItineraryGroqClient, GroqItineraryClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<ItineraryPlanningOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.GroqRequestTimeoutSeconds));
        });

        return services;
    }

    private static string FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}
