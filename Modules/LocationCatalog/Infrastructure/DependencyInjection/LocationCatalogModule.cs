using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Infrastructure.Persistence;
using Glinter.Modules.LocationCatalog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Glinter.Modules.LocationCatalog.Application.Locations;
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
        services.AddScoped<GetCountriesQueryHandler>();
        services.AddScoped<GetGovernoratesByCountryQueryHandler>();
        services.AddScoped<GetDistrictsByGovernorateQueryHandler>();
        services.AddScoped<GetAreasByDistrictQueryHandler>();
        services.AddScoped<SearchAreasQueryHandler>();
        services.AddScoped<GetAreaByIdQueryHandler>();

        services.AddDbContext<LocationCatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ILocationCatalogDbContext>(provider =>
            provider.GetRequiredService<LocationCatalogDbContext>());

        services.AddScoped<ILocationCatalogReadService, LocationCatalogReadService>();

        return services;
    }
}