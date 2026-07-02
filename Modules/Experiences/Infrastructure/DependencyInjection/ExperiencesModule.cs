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

        services.AddScoped<ExperienceService>();
        services.AddScoped<ExperienceRecommendationService>();
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
