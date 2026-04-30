using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence;

public class ProfilesDbContext : DbContext, IProfilesDbContext
{
    public ProfilesDbContext(DbContextOptions<ProfilesDbContext> options)
        : base(options)
    {
    }

    public DbSet<TravelerProfile> TravelerProfiles => Set<TravelerProfile>();

    public DbSet<LocalBuddyProfile> LocalBuddyProfiles => Set<LocalBuddyProfile>();

    public DbSet<HotelOwnerProfile> HotelOwnerProfiles => Set<HotelOwnerProfile>();

    public DbSet<ExperienceProviderProfile> ExperienceProviderProfiles => Set<ExperienceProviderProfile>();

    public DbSet<Interest> Interests => Set<Interest>();

    public DbSet<BuddyInterest> BuddyInterests => Set<BuddyInterest>();

    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ProfilesDbContext).Assembly);
    }
}