using System.Security.Claims;
using System.Threading.RateLimiting;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Options;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.Experiences.Infrastructure.External.Groq;
using Glinter.Modules.Experiences.Infrastructure.Files;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Experiences.Infrastructure.DependencyInjection;

public static class ExperiencesModule
{
    public static IServiceCollection AddExperiencesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ExperiencesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<ExperienceRecommendationOptions>(
            configuration.GetSection(ExperienceRecommendationOptions.SectionName));
        services.PostConfigure<ExperienceRecommendationOptions>(options =>
        {
            options.GroqApiKey = FirstConfigured(
                options.GroqApiKey,
                configuration["GROQ_API_KEY"],
                configuration["HotelRecommendations:GroqApiKey"],
                configuration["SafetyIndex:GroqApiKey"]);
            options.GroqModel = FirstConfigured(
                options.GroqModel,
                configuration["GROQ_MODEL"],
                configuration["HotelRecommendations:GroqModel"],
                configuration["SafetyIndex:GroqModel"],
                "llama-3.1-8b-instant");
            options.GroqBaseUrl = FirstConfigured(
                options.GroqBaseUrl,
                configuration["GROQ_BASE_URL"],
                configuration["HotelRecommendations:GroqBaseUrl"],
                configuration["SafetyIndex:GroqChatCompletionsUrl"],
                "https://api.groq.com/openai/v1");
        });

        var rateLimitPermit = configuration.GetValue<int?>(
            "ExperienceRecommendations:RateLimiting:PermitLimit") ?? 10;
        var rateLimitWindowSeconds = configuration.GetValue<int?>(
            "ExperienceRecommendations:RateLimiting:WindowSeconds") ?? 60;
        var naturalLanguageMaxCharacters = configuration.GetValue<int?>(
            "ExperienceRecommendations:NaturalLanguageMaxCharacters") ?? 1000;
        var maxRequestedCategories = configuration.GetValue<int?>(
            "ExperienceRecommendations:MaxRequestedCategories") ?? 5;

        if (rateLimitPermit <= 0 || rateLimitWindowSeconds <= 0 ||
            naturalLanguageMaxCharacters <= 0 || maxRequestedCategories <= 0)
        {
            throw new InvalidOperationException(
                "Experience recommendation limits must all be greater than zero.");
        }

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                ExperienceRecommendationRateLimitPolicies.AiRequests,
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

        services.AddScoped<ExperienceService>();
        services.AddScoped<IExperiencesAdminReadService, ExperiencesAdminReadService>();
        services.AddScoped<ExperienceRecommendationService>();
        services.AddScoped<IExperienceRecommendationService>(
            provider => provider.GetRequiredService<ExperienceRecommendationService>());
        services.AddScoped<
            IExperienceRecommendationReadService,
            ExperienceRecommendationReadService>();
        services.AddSingleton<ExperienceImageStorage>();

        services.AddHttpClient<IExperienceRecommendationGroqClient, GroqExperienceRecommendationClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<ExperienceRecommendationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.GroqRequestTimeoutSeconds));
        });

        return services;
    }

    private static string FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}
