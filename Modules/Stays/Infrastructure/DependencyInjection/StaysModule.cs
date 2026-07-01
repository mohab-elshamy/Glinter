using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Options;
using Glinter.Modules.Stays.Application.Services;
using Glinter.Modules.Stays.Infrastructure.External.Groq;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        services.AddScoped<StayService>();
        services.AddScoped<HotelRecommendationService>();

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
