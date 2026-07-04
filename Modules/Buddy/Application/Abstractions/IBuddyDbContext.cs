using Glinter.Modules.Buddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Buddy.Application.Abstractions;

public interface IBuddyDbContext
{
    DbSet<BuddyAvailability> BuddyAvailabilities { get; }
    DbSet<BuddyBooking> BuddyBookings { get; }
    DbSet<BuddyReview> BuddyReviews { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
