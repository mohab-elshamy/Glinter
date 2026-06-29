using Glinter.Modules.Stays.Application.Services;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.DependencyInjection;

public static class StaysModule
{
    public static IServiceCollection AddStaysModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StaysDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<StayService>();

        return services;
    }
}
