using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glinter.Modules.Experiences.Infrastructure.DependencyInjection;

public static class ExperiencesModule
{
    public static IServiceCollection AddExperiencesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddDbContext<ExperiencesDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IExperiencesDbContext>(provider =>
            provider.GetRequiredService<ExperiencesDbContext>());

        services.AddScoped<IExperienceRepository, ExperienceRepository>();
        services.AddScoped<IExperienceAvailabilityRepository, ExperienceAvailabilityRepository>();
        services.AddScoped<IExperienceBookingRepository, ExperienceBookingRepository>();
        services.AddScoped<IExperienceReviewRepository, ExperienceReviewRepository>();
        services.AddScoped<IExperienceCategoryRepository, ExperienceCategoryRepository>();
        services.AddScoped<IVibeRepository, VibeRepository>();

        return services;
    }
}