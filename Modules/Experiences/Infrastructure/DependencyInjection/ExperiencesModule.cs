using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperience;
using Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceActiveStatus;
using Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetAllExperiences;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceById;
using Glinter.Modules.Experiences.Infrastructure.Services;

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
        
        services.AddScoped<IExperienceProfileResolver, ExperienceProfileResolver>();

        services.AddScoped<CreateExperienceCommandValidator>();
        services.AddScoped<CreateExperienceCommandHandler>();

        services.AddScoped<UpdateExperienceCommandValidator>();
        services.AddScoped<UpdateExperienceCommandHandler>();

        services.AddScoped<SetExperienceActiveStatusCommandValidator>();
        services.AddScoped<SetExperienceActiveStatusCommandHandler>();

        services.AddScoped<GetAllExperiencesQueryHandler>();
        services.AddScoped<GetExperienceByIdQueryHandler>();
        return services;
    }
}