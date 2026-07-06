using Glinter.Modules.ComfortIndex.Application.Abstractions;
using Glinter.Modules.ComfortIndex.Application.Services;

namespace Glinter.Modules.ComfortIndex.Infrastructure.DependencyInjection;

public static class ComfortIndexModule
{
    public static IServiceCollection AddComfortIndexModule(
        this IServiceCollection services)
    {
        services.AddScoped<IComfortIndexService, ComfortIndexService>();

        return services;
    }
}
