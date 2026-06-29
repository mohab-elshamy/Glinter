using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.DependencyInjection;

public static class ExperiencesModule
{
    public static IServiceCollection AddExperiencesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ExperiencesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ExperienceService>();

        return services;
    }
}
