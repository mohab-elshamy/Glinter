using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Options;
using Glinter.Modules.Stays.Application.Services;
using Glinter.Modules.Stays.Infrastructure.External.Groq;
using Glinter.Modules.Stays.Infrastructure.Files;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Glinter.Modules.Stays.Infrastructure.DependencyInjection;

public static class StaysModule
{
    public static IServiceCollection AddStaysModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StaysDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<HotelRecommendationOptions>(
            configuration.GetSection(HotelRecommendationOptions.SectionName));
        services.PostConfigure<HotelRecommendationOptions>(options =>
        {
            options.GroqApiKey = FirstConfigured(
                options.GroqApiKey,
                configuration["GROQ_API_KEY"],
                configuration["SafetyIndex:GroqApiKey"]);
            options.GroqModel = FirstConfigured(
                options.GroqModel,
                configuration["GROQ_MODEL"],
                configuration["SafetyIndex:GroqModel"],
                "llama-3.1-8b-instant");
            options.GroqBaseUrl = FirstConfigured(
                options.GroqBaseUrl,
                configuration["GROQ_BASE_URL"],
                configuration["SafetyIndex:GroqChatCompletionsUrl"],
                "https://api.groq.com/openai/v1");
        });

        var rateLimitPermit = configuration.GetValue<int?>(
            "HotelRecommendations:RateLimiting:PermitLimit") ?? 10;
        var rateLimitWindowSeconds = configuration.GetValue<int?>(
            "HotelRecommendations:RateLimiting:WindowSeconds") ?? 60;
        var naturalLanguageMaxCharacters = configuration.GetValue<int?>(
            "HotelRecommendations:NaturalLanguageMaxCharacters") ?? 1000;
        var maxRequestedAmenities = configuration.GetValue<int?>(
            "HotelRecommendations:MaxRequestedAmenities") ?? 20;

        if (rateLimitPermit <= 0)
            throw new InvalidOperationException(
                "HotelRecommendations:RateLimiting:PermitLimit must be greater than zero.");
        if (rateLimitWindowSeconds <= 0)
            throw new InvalidOperationException(
                "HotelRecommendations:RateLimiting:WindowSeconds must be greater than zero.");
        if (naturalLanguageMaxCharacters <= 0)
            throw new InvalidOperationException(
                "HotelRecommendations:NaturalLanguageMaxCharacters must be greater than zero.");
        if (maxRequestedAmenities <= 0)
            throw new InvalidOperationException(
                "HotelRecommendations:MaxRequestedAmenities must be greater than zero.");

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                HotelRecommendationRateLimitPolicies.AiRequests,
                httpContext =>
                {
                    var userId = httpContext.User.FindFirstValue(
                        ClaimTypes.NameIdentifier);
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
                            QueueProcessingOrder =
                                QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        });
                });
        });

        services.AddScoped<StayService>();
        services.AddScoped<IStaysAdminReadService, StaysAdminReadService>();
        services.AddScoped<HotelRecommendationService>();
        services.AddSingleton<StayImageStorage>();

        services.AddHttpClient<IHotelRecommendationGroqClient, GroqHotelRecommendationClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<HotelRecommendationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.GroqRequestTimeoutSeconds));
        });

        return services;
    }

    private static string FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}
