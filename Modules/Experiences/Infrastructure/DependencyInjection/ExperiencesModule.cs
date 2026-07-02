using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Services;
using Glinter.Modules.Experiences.Infrastructure.Files;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Services;
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
        services.AddScoped<
            IExperienceRecommendationReadService,
            ExperienceRecommendationReadService>();
        services.AddSingleton<ExperienceImageStorage>();

        return services;
    }
}
