using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Application.Abstractions;

public interface IProfilesDbContext
{
    DbSet<TravelerProfile> TravelerProfiles { get; }

    DbSet<LocalBuddyProfile> LocalBuddyProfiles { get; }

    DbSet<HotelOwnerProfile> HotelOwnerProfiles { get; }

    DbSet<ExperienceProviderProfile> ExperienceProviderProfiles { get; }

    DbSet<Interest> Interests { get; }

    DbSet<BuddyInterest> BuddyInterests { get; }

    DbSet<TravelerInterest> TravelerInterests { get; }

    DbSet<UserFollow> UserFollows { get; }
    DbSet<LocalBuddyVerificationEvent> LocalBuddyVerificationEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
