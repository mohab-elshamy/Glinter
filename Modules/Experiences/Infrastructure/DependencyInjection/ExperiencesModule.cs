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
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.DeactivateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.ActivateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CancelExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CompleteExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceBookings;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Commands.DeleteExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceReviews;

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
        services.AddHttpContextAccessor();
        services.AddScoped<IExperienceProfileResolver, ExperienceProfileResolver>();
        services.AddScoped<CreateExperienceCommandValidator>();
        services.AddScoped<CreateExperienceCommandHandler>();
        services.AddScoped<UpdateExperienceCommandValidator>();
        services.AddScoped<UpdateExperienceCommandHandler>();
        services.AddScoped<SetExperienceActiveStatusCommandValidator>();
        services.AddScoped<SetExperienceActiveStatusCommandHandler>();
        services.AddScoped<GetAllExperiencesQueryHandler>();
        services.AddScoped<GetExperienceByIdQueryHandler>();
        services.AddScoped<CreateExperienceAvailabilityCommandValidator>();
        services.AddScoped<CreateExperienceAvailabilityCommandHandler>();
        services.AddScoped<DeactivateExperienceAvailabilityCommandValidator>();
        services.AddScoped<DeactivateExperienceAvailabilityCommandHandler>();
        services.AddScoped<GetExperienceAvailabilityQueryHandler>();
        services.AddScoped<ActivateExperienceAvailabilityCommandValidator>();
        services.AddScoped<ActivateExperienceAvailabilityCommandHandler>();
        services.AddScoped<CreateExperienceBookingCommandValidator>();
        services.AddScoped<CreateExperienceBookingCommandHandler>();

        services.AddScoped<CancelExperienceBookingCommandValidator>();
        services.AddScoped<CancelExperienceBookingCommandHandler>();

        services.AddScoped<CompleteExperienceBookingCommandValidator>();
        services.AddScoped<CompleteExperienceBookingCommandHandler>();

        services.AddScoped<GetExperienceBookingsQueryHandler>();
        
        services.AddScoped<CreateExperienceReviewCommandValidator>();
        services.AddScoped<CreateExperienceReviewCommandHandler>();

        services.AddScoped<UpdateExperienceReviewCommandValidator>();
        services.AddScoped<UpdateExperienceReviewCommandHandler>();

        services.AddScoped<DeleteExperienceReviewCommandValidator>();
        services.AddScoped<DeleteExperienceReviewCommandHandler>();

        services.AddScoped<GetExperienceReviewsQueryHandler>();
        return services;
    }
}