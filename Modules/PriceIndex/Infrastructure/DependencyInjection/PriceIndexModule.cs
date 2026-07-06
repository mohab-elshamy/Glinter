using Glinter.Modules.PriceIndex.Application.Abstractions;
using Glinter.Modules.PriceIndex.Application.Services;

namespace Glinter.Modules.PriceIndex.Infrastructure.DependencyInjection;

public static class PriceIndexModule
{
    public static IServiceCollection AddPriceIndexModule(
        this IServiceCollection services)
    {
        services.AddScoped<IPriceIndexService, PriceIndexService>();

        return services;
    }
}
