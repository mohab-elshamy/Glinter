using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Options;
using Glinter.Modules.SafetyIndex.Application.Services;
using Glinter.Modules.SafetyIndex.Infrastructure.External.GoogleNews;
using Glinter.Modules.SafetyIndex.Infrastructure.External.Groq;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence.Repositories;
using Glinter.Modules.SafetyIndex.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.DependencyInjection;

public static class SafetyIndexModule
{
    public static IServiceCollection AddSafetyIndexModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found.");

        services.Configure<SafetyIndexOptions>(
            configuration.GetSection(SafetyIndexOptions.SectionName));

        services.AddDbContext<SafetyIndexDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ISafetyIndexRepository, SafetyIndexRepository>();
        services.AddScoped<INewsHistoryStore, NewsHistoryStore>();
        services.AddScoped<ISafetyIndexService, SafetyIndexService>();
        services.AddSingleton<GroqRequestRateLimiter>();

        services.AddHttpClient<IGoogleNewsRssClient, GoogleNewsRssClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<SafetyIndexOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.RssRequestTimeoutSeconds));
        });

        services.AddHttpClient<ISafetyScoringClient, GroqSafetyScoringClient>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<SafetyIndexOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuredOptions.GroqRequestTimeoutSeconds));
        });

        services.AddHostedService<SafetyIndexHostedService>();

        return services;
    }
}
