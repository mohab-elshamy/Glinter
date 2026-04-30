using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertTravelerProfile;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetMyProfile;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertLocalBuddyProfile;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetInterests;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetUserProfileById;
using Glinter.Modules.Profiles.Application.Profiles.Commands.FollowUser;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UnfollowUser;
using Glinter.Modules.Profiles.Application.Profiles.Queries.GetFollowStatus;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertHotelOwnerProfile;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertExperienceProviderProfile;
using Glinter.Modules.Profiles.Application.Common.Services;
using Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateProfileImage;

namespace Glinter.Modules.Profiles.Infrastructure.DependencyInjection;

public static class ProfilesModule
{
    public static IServiceCollection AddProfilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection not found.");

        services.AddDbContext<ProfilesDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProfilesDbContext>(sp =>
            sp.GetRequiredService<ProfilesDbContext>());

        services.AddScoped<UpsertTravelerProfileCommandHandler>();
        services.AddScoped<GetMyProfileQueryHandler>();
        services.AddScoped<UpsertLocalBuddyProfileCommandHandler>();
        services.AddScoped<GetInterestsQueryHandler>();
        services.AddScoped<GetLocalBuddiesQueryHandler>();
        services.AddScoped<GetUserProfileByIdQueryHandler>();
        services.AddScoped<FollowUserCommandHandler>();
        services.AddScoped<UnfollowUserCommandHandler>();
        services.AddScoped<GetFollowStatusQueryHandler>();
        services.AddScoped<UpsertHotelOwnerProfileCommandHandler>();
        services.AddScoped<UpsertExperienceProviderProfileCommandHandler>();
        services.AddScoped<ProfileFollowStatsService>();
        services.AddScoped<UpdateProfileImageCommandHandler>();
        
        return services;
    }
}