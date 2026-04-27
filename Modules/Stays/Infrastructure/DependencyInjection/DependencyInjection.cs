using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Listings.Commands;
using Glinter.Modules.Stays.Application.Listings.Queries;
using Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Glinter.Modules.Stays.Application.Bookings.Commands;
using Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;
using Glinter.Modules.Stays.Application.Bookings.Queries;
using Glinter.Modules.Stays.Application.Bookings.Commands;
namespace Glinter.Modules.Stays.Infrastructure.DependencyInjection;
using Glinter.Modules.Stays.Application.Listings.Commands;
using Glinter.Modules.Stays.Application.Reviews.Commands;
using Glinter.Modules.Stays.Application.Reviews.Queries;
using Glinter.Modules.Stays.Infrastructure.Persistence.Repositories;
using Glinter.Modules.Stays.Application.Listings.Queries;

public static class DependencyInjection
{
    public static IServiceCollection AddStaysModule(this IServiceCollection services)
    {
        services.AddScoped<IStayRepository, StayRepository>();
        services.AddScoped<UpdateStayReviewHandler>();
        services.AddScoped<CreateStayHandler>();
        services.AddScoped<GetAllStaysHandler>();
        services.AddScoped<GetStayByIdHandler>();
        services.AddScoped<UpdateStayHandler>();
        services.AddScoped<IStayBookingRepository, StayBookingRepository>();
        services.AddScoped<CreateStayBookingHandler>();
        services.AddScoped<GetStayBookingsHandler>();
        services.AddScoped<CancelStayBookingHandler>();
        services.AddScoped<IStayReviewRepository, StayReviewRepository>();
        services.AddScoped<CreateStayReviewHandler>();
        services.AddScoped<GetStayReviewsHandler>();
        services.AddScoped<DeleteStayReviewHandler>();
        services.AddScoped<GetStaysByAreaHandler>();
        services.AddScoped<SetStayActiveStatusHandler>();

        return services;
    }
}