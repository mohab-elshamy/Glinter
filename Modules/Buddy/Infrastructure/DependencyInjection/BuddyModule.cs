using Glinter.Modules.Buddy.Application.Abstractions;
using Glinter.Modules.Buddy.Application.Requests;
using Glinter.Modules.Buddy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Buddy.Infrastructure.DependencyInjection;

public static class BuddyModule
{
    public static IServiceCollection AddBuddyModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection not found.");

        services.AddDbContext<BuddyDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddScoped<IBuddyDbContext>(
            provider => provider.GetRequiredService<BuddyDbContext>());
        services.AddScoped<BuddyRequestService>();
        return services;
    }
}
