using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Locations;
using Glinter.Modules.LocationCatalog.Application.Safety.Services;
using Glinter.Modules.LocationCatalog.Infrastructure.Options;
using Glinter.Modules.LocationCatalog.Infrastructure.Persistence;
using Glinter.Modules.LocationCatalog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.LocationCatalog.Infrastructure.DependencyInjection;

public static class LocationCatalogModule
{
    public static IServiceCollection AddLocationCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection was not found.");
        }

        services.AddDbContext<LocationCatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILocationCatalogDbContext>(provider =>
            provider.GetRequiredService<LocationCatalogDbContext>());

        services.AddScoped<ILocationCatalogReadService, LocationCatalogReadService>();

        // Location query handlers
        services.AddScoped<GetCountriesQueryHandler>();
        services.AddScoped<GetGovernoratesByCountryQueryHandler>();
        services.AddScoped<GetDistrictsByGovernorateQueryHandler>();
        services.AddScoped<GetAreasByDistrictQueryHandler>();
        services.AddScoped<SearchAreasQueryHandler>();
        services.AddScoped<GetAreaByIdQueryHandler>();
        services.AddScoped<SearchDistrictsQueryHandler>();
        services.AddScoped<GetDistrictIndexByDistrictQueryHandler>();
        services.AddScoped<GetGovernorateDistrictIndicesQueryHandler>();

        // Safety AI services
        services.Configure<GroqOptions>(configuration.GetSection("Groq"));

        services.AddHttpClient<GroqSafetyAiAnalyzer>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<GroqOptions>>().Value;

            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.groq.com/openai/v1/"
                : options.BaseUrl;

            client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : $"{baseUrl}/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<ISafetyAiAnalyzer>(provider =>
            provider.GetRequiredService<GroqSafetyAiAnalyzer>());

        services.AddScoped<DistrictSafetySignalService>();
        services.AddScoped<DistrictSafetyScoreService>();

        return services;
    }
}