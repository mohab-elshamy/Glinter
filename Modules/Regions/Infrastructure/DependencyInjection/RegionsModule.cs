using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Countries.Commands;
using Glinter.Modules.Regions.Application.Countries.Queries;
using Glinter.Modules.Regions.Application.Districts.Commands;
using Glinter.Modules.Regions.Application.Districts.Queries;
using Glinter.Modules.Regions.Application.Governorates.Commands;
using Glinter.Modules.Regions.Application.Governorates.Queries;
using Glinter.Modules.Regions.Application.Neighbourhoods.Commands;
using Glinter.Modules.Regions.Application.Neighbourhoods.Queries;
using Glinter.Modules.Regions.Application.PointLookup.Queries;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;
using Glinter.Modules.Regions.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.DependencyInjection;

public static class RegionsModule
{
    public static IServiceCollection AddRegionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found.");

        services.AddDbContext<RegionsDbContext>(options =>
            options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()));

        // Repositories
        services.AddScoped<IAdm0Repository, Adm0Repository>();
        services.AddScoped<IAdm1Repository, Adm1Repository>();
        services.AddScoped<IAdm2Repository, Adm2Repository>();
        services.AddScoped<IAdm3Repository, Adm3Repository>();
        services.AddScoped<IRegionsPointLookupRepository, RegionsPointLookupRepository>();
        services.AddScoped<IRegionReferenceService, RegionReferenceService>();

        // Services
        services.AddScoped<IGeoJsonImportService, GeoJsonImportService>();
        services.AddScoped<LocalFileImportService>();
        services.AddScoped<GetRegionsByPointHandler>();

        // Country handlers
        services.AddScoped<CreateCountryHandler>();
        services.AddScoped<UpdateCountryHandler>();
        services.AddScoped<DeleteCountryHandler>();
        services.AddScoped<GetCountryByIdHandler>();
        services.AddScoped<GetAllCountriesHandler>();

        // Governorate handlers
        services.AddScoped<CreateGovernorateHandler>();
        services.AddScoped<UpdateGovernorateHandler>();
        services.AddScoped<DeleteGovernorateHandler>();
        services.AddScoped<GetGovernorateByIdHandler>();
        services.AddScoped<GetAllGovernoratesHandler>();
        services.AddScoped<GetGovernoratesByCountryHandler>();

        // District handlers
        services.AddScoped<CreateDistrictHandler>();
        services.AddScoped<UpdateDistrictHandler>();
        services.AddScoped<DeleteDistrictHandler>();
        services.AddScoped<GetDistrictByIdHandler>();
        services.AddScoped<GetAllDistrictsHandler>();
        services.AddScoped<GetDistrictsByGovernorateHandler>();

        // Neighbourhood handlers
        services.AddScoped<CreateNeighbourhoodHandler>();
        services.AddScoped<UpdateNeighbourhoodHandler>();
        services.AddScoped<DeleteNeighbourhoodHandler>();
        services.AddScoped<GetNeighbourhoodByIdHandler>();
        services.AddScoped<GetAllNeighbourhoodsHandler>();
        services.AddScoped<GetNeighbourhoodsByDistrictHandler>();

        return services;
    }
}
