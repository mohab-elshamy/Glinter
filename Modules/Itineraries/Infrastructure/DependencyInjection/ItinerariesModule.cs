using System.Security.Claims;
using System.Threading.RateLimiting;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Application.Services;
using Glinter.Modules.Itineraries.Infrastructure.External.Groq;
using Glinter.Modules.Itineraries.Infrastructure.Routing;
using Glinter.Modules.Itineraries.Infrastructure.Persistence;
using Glinter.Modules.Itineraries.Infrastructure.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.DependencyInjection;

public static class ItinerariesModule
{
    public static IServiceCollection AddItinerariesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ItinerariesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<ItineraryPlanningOptions>(
            configuration.GetSection(ItineraryPlanningOptions.SectionName));
        services.Configure<WeatherOptions>(
            configuration.GetSection(WeatherOptions.SectionName));
        services.AddMemoryCache();
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

        var rateLimitPermit = configuration.GetValue<int?>(
            "ItineraryPlanning:RateLimiting:PermitLimit") ?? 10;
        var rateLimitWindowSeconds = configuration.GetValue<int?>(
            "ItineraryPlanning:RateLimiting:WindowSeconds") ?? 60;
        var naturalLanguageMaxCharacters = configuration.GetValue<int?>(
            "ItineraryPlanning:NaturalLanguageMaxCharacters") ?? 1500;
        var maxSelectedCategories = configuration.GetValue<int?>(
            "ItineraryPlanning:MaxSelectedCategories") ?? 5;

        if (rateLimitPermit <= 0 || rateLimitWindowSeconds <= 0 ||
            naturalLanguageMaxCharacters <= 0 || maxSelectedCategories <= 0)
        {
            throw new InvalidOperationException(
                "Itinerary planning limits must all be greater than zero.");
        }

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                ItineraryPlanningRateLimitPolicies.AiRequests,
                httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var partitionKey = !string.IsNullOrWhiteSpace(userId)
                        ? $"user:{userId}"
                        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimitPermit,
                            Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        });
                });
        });

        services.AddScoped<ItineraryPlannerService>();
        services.AddScoped<IItineraryPlannerService>(
            provider => provider.GetRequiredService<ItineraryPlannerService>());
        services.AddScoped<IItineraryRoutePlanner, ItineraryRoutePlanner>();
        services.AddScoped<SavedItineraryService>();
        services.AddHttpClient<IWeatherForecastService, OpenMeteoWeatherForecastService>(
            (serviceProvider, client) =>
            {
                var weather = serviceProvider
                    .GetRequiredService<IOptions<WeatherOptions>>()
                    .Value;
                client.BaseAddress = new Uri(weather.BaseUrl.TrimEnd('/'));
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, weather.TimeoutSeconds));
            });

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
