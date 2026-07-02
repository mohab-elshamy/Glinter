using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Domain.Enums;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Domain.Entities;
using Glinter.Modules.Profiles.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Glinter.Modules.Profiles.Application.Profiles.Services;

public sealed class BuddyEngagementService(
    IProfilesDbContext dbContext,
    ICurrentUserService currentUser,
    CreateNotificationHandler createNotificationHandler)
{
    private static readonly BuddyBookingStatus[] BlockingStatuses =
        [BuddyBookingStatus.Pending, BuddyBookingStatus.Accepted];

    public async Task<List<BuddyAvailabilityResponse>> GetAvailabilityAsync(
        Guid buddyUserId,
        bool manage,
        CancellationToken cancellationToken)
    {
        var buddy = await GetBuddyAsync(buddyUserId, cancellationToken);
        if (manage)
            EnsureBuddyOwner(buddyUserId);
        else if (buddy.VerificationStatus != VerificationStatus.Approved)
            throw new NotFoundException("Local buddy profile not found.");

        var query = dbContext.BuddyAvailabilities
            .AsNoTracking()
            .Where(x => x.LocalBuddyUserId == buddyUserId);

        if (!manage)
            query = query.Where(x => x.IsActive && x.EndTimeUtc > DateTime.UtcNow);

        return await query
            .OrderBy(x => x.StartTimeUtc)
            .Select(x => new BuddyAvailabilityResponse
            {
                Id = x.Id,
                LocalBuddyUserId = x.LocalBuddyUserId,
                StartTimeUtc = x.StartTimeUtc,
                EndTimeUtc = x.EndTimeUtc,
                Price = x.Price,
                IsActive = x.IsActive,
                IsBooked = x.Bookings.Any(b => BlockingStatuses.Contains(b.Status))
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BuddyAvailabilityResponse> CreateAvailabilityAsync(
        Guid buddyUserId,
        BuddyAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        EnsureBuddyOwner(buddyUserId);
        await GetBuddyAsync(buddyUserId, cancellationToken);
        ValidateAvailability(request);

        var overlaps = await dbContext.BuddyAvailabilities.AnyAsync(
            x => x.LocalBuddyUserId == buddyUserId &&
                 x.IsActive &&
                 request.StartTimeUtc < x.EndTimeUtc &&
                 request.EndTimeUtc > x.StartTimeUtc,
            cancellationToken);
        if (overlaps)
            throw new ValidationException("This availability overlaps an active slot.");

        var availability = new BuddyAvailability
        {
            Id = Guid.NewGuid(),
            LocalBuddyUserId = buddyUserId,
            StartTimeUtc = request.StartTimeUtc.ToUniversalTime(),
            EndTimeUtc = request.EndTimeUtc.ToUniversalTime(),
            Price = request.Price
        };
        dbContext.BuddyAvailabilities.Add(availability);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAvailability(availability, false);
    }

    public async Task<BuddyAvailabilityResponse> UpdateAvailabilityAsync(
        Guid availabilityId,
        BuddyAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAvailability(request);
        var availability = await dbContext.BuddyAvailabilities
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(x => x.Id == availabilityId, cancellationToken)
            ?? throw new NotFoundException("Buddy availability was not found.");
        EnsureBuddyOwner(availability.LocalBuddyUserId);

        if (availability.Bookings.Any(x => BlockingStatuses.Contains(x.Status)))
            throw new ValidationException("A booked availability cannot be rescheduled.");

        var overlaps = await dbContext.BuddyAvailabilities.AnyAsync(
            x => x.Id != availabilityId &&
                 x.LocalBuddyUserId == availability.LocalBuddyUserId &&
                 x.IsActive &&
                 request.StartTimeUtc < x.EndTimeUtc &&
                 request.EndTimeUtc > x.StartTimeUtc,
            cancellationToken);
        if (overlaps)
            throw new ValidationException("This availability overlaps an active slot.");

        availability.StartTimeUtc = request.StartTimeUtc.ToUniversalTime();
        availability.EndTimeUtc = request.EndTimeUtc.ToUniversalTime();
        availability.Price = request.Price;
        availability.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAvailability(availability, false);
    }

    public async Task<BuddyAvailabilityResponse> SetAvailabilityActiveAsync(
        Guid availabilityId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var availability = await dbContext.BuddyAvailabilities
            .Include(x => x.Bookings)
            .FirstOrDefaultAsync(x => x.Id == availabilityId, cancellationToken)
            ?? throw new NotFoundException("Buddy availability was not found.");
        EnsureBuddyOwner(availability.LocalBuddyUserId);
        if (!isActive && availability.Bookings.Any(x => BlockingStatuses.Contains(x.Status)))
            throw new ValidationException("A booked availability cannot be deactivated.");

        availability.IsActive = isActive;
        availability.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAvailability(
            availability,
            availability.Bookings.Any(x => BlockingStatuses.Contains(x.Status)));
    }

    public async Task<BuddyBookingResponse> CreateBookingAsync(
        Guid buddyUserId,
        CreateBuddyBookingRequest request,
        CancellationToken cancellationToken)
    {
        var travelerUserId = RequireRole(RoleNames.Traveler);
        if (request.AvailabilityId == Guid.Empty)
            throw new ValidationException("AvailabilityId is required.");
        if (request.Notes?.Length > 1000)
            throw new ValidationException("Notes cannot exceed 1000 characters.");

        var buddy = await GetBuddyAsync(buddyUserId, cancellationToken);
        if (buddy.VerificationStatus != VerificationStatus.Approved)
            throw new NotFoundException("Local buddy profile not found.");
        var traveler = await dbContext.TravelerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == travelerUserId, cancellationToken)
            ?? throw new ValidationException("Create your traveler profile before requesting a buddy.");
        var availability = await dbContext.BuddyAvailabilities
            .FirstOrDefaultAsync(
                x => x.Id == request.AvailabilityId &&
                     x.LocalBuddyUserId == buddyUserId,
                cancellationToken)
            ?? throw new NotFoundException("Buddy availability was not found.");

        if (!availability.IsActive || availability.StartTimeUtc <= DateTime.UtcNow)
            throw new ValidationException("This buddy availability is no longer bookable.");
        if (await dbContext.BuddyBookings.AnyAsync(
                x => x.AvailabilityId == availability.Id &&
                     BlockingStatuses.Contains(x.Status),
                cancellationToken))
        {
            throw new ValidationException("This buddy availability has already been requested.");
        }

        var booking = new BuddyBooking
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            LocalBuddyUserId = buddyUserId,
            TravelerUserId = travelerUserId,
            TotalPrice = availability.Price,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        dbContext.BuddyBookings.Add(booking);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation,
                      ConstraintName: "IX_buddy_bookings_AvailabilityId"
                  })
        {
            throw new ValidationException(
                "This buddy availability has already been requested.");
        }
        booking.Availability = availability;
        await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = buddyUserId,
                Type = NotificationType.Booking,
                Title = "New buddy booking request",
                Body = $"{traveler.DisplayName} requested one of your available times.",
                LinkUrl = "/profile/me?tab=buddy-schedule",
                SourceModule = "Profiles",
                SourceEntityType = "BuddyBooking",
                SourceEntityId = booking.Id
            },
            cancellationToken);
        return MapBooking(booking, buddy.DisplayName, traveler.DisplayName);
    }

    public async Task<List<BuddyBookingResponse>> GetMyBookingsAsync(
        CancellationToken cancellationToken)
    {
        var userId = RequireRole(RoleNames.Traveler);
        var bookings = await dbContext.BuddyBookings
            .AsNoTracking()
            .Include(x => x.Availability)
            .Where(x => x.TravelerUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return await MapBookingsAsync(bookings, cancellationToken);
    }

    public async Task<List<BuddyBookingResponse>> GetBuddyBookingsAsync(
        Guid buddyUserId,
        CancellationToken cancellationToken)
    {
        EnsureBuddyOwnerOrAdmin(buddyUserId);
        var bookings = await dbContext.BuddyBookings
            .AsNoTracking()
            .Include(x => x.Availability)
            .Where(x => x.LocalBuddyUserId == buddyUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return await MapBookingsAsync(bookings, cancellationToken);
    }

    public async Task<BuddyBookingResponse> UpdateBookingStatusAsync(
        Guid bookingId,
        UpdateBuddyBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await dbContext.BuddyBookings
            .Include(x => x.Availability)
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Buddy booking was not found.");
        EnsureBuddyOwnerOrAdmin(booking.LocalBuddyUserId);
        if (!Enum.TryParse<BuddyBookingStatus>(request.Status, true, out var status) ||
            status is BuddyBookingStatus.Pending or BuddyBookingStatus.Cancelled)
        {
            throw new ValidationException("Status must be Accepted, Rejected, or Completed.");
        }
        if (booking.Status == BuddyBookingStatus.Pending &&
            status is not (BuddyBookingStatus.Accepted or BuddyBookingStatus.Rejected))
        {
            throw new ValidationException("A pending request can only be accepted or rejected.");
        }
        if (booking.Status == BuddyBookingStatus.Accepted &&
            status != BuddyBookingStatus.Completed)
        {
            throw new ValidationException("An accepted request can only be completed.");
        }
        if (booking.Status is BuddyBookingStatus.Rejected or BuddyBookingStatus.Cancelled or BuddyBookingStatus.Completed)
            throw new ValidationException("This buddy request is already final.");

        booking.Status = status;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = booking.TravelerUserId,
                Type = NotificationType.Booking,
                Title = $"Buddy request {status.ToString().ToLowerInvariant()}",
                Body = $"Your local buddy request is now {status.ToString().ToLowerInvariant()}.",
                LinkUrl = "/profile/me?tab=bookings",
                SourceModule = "Profiles",
                SourceEntityType = "BuddyBooking",
                SourceEntityId = booking.Id
            },
            cancellationToken);
        return await MapBookingAsync(booking, cancellationToken);
    }

    public async Task<BuddyBookingResponse> CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var travelerUserId = RequireRole(RoleNames.Traveler);
        var booking = await dbContext.BuddyBookings
            .Include(x => x.Availability)
            .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException("Buddy booking was not found.");
        if (booking.TravelerUserId != travelerUserId)
            throw new ForbiddenException("You cannot cancel another traveler's buddy request.");
        if (booking.Status is not (BuddyBookingStatus.Pending or BuddyBookingStatus.Accepted))
            throw new ConflictException("This buddy request can no longer be cancelled.");
        if (booking.Availability.StartTimeUtc <= DateTime.UtcNow)
            throw new ConflictException("A buddy request cannot be cancelled after its start time.");

        booking.Status = BuddyBookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = booking.LocalBuddyUserId,
                Type = NotificationType.Booking,
                Title = "Buddy request cancelled",
                Body = "A traveler cancelled their local buddy request.",
                LinkUrl = "/profile/me?tab=buddy-schedule",
                SourceModule = "Profiles",
                SourceEntityType = "BuddyBooking",
                SourceEntityId = booking.Id
            },
            cancellationToken);
        return await MapBookingAsync(booking, cancellationToken);
    }

    public async Task<List<BuddyReviewResponse>> GetReviewsAsync(
        Guid buddyUserId,
        CancellationToken cancellationToken)
    {
        var buddy = await GetBuddyAsync(buddyUserId, cancellationToken);
        if (buddy.VerificationStatus != VerificationStatus.Approved)
            throw new NotFoundException("Local buddy profile not found.");
        var reviews = await dbContext.BuddyReviews
            .AsNoTracking()
            .Where(x => x.LocalBuddyUserId == buddyUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var travelerNames = await GetTravelerNamesAsync(
            reviews.Select(x => x.TravelerUserId),
            cancellationToken);
        return reviews.Select(x => MapReview(
            x,
            travelerNames.GetValueOrDefault(x.TravelerUserId, "Traveler"))).ToList();
    }

    public async Task<BuddyReviewResponse> CreateReviewAsync(
        Guid buddyUserId,
        CreateBuddyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var travelerUserId = RequireRole(RoleNames.Traveler);
        if (request.BookingId == Guid.Empty)
            throw new ValidationException("BookingId is required.");
        if (request.Rating is < 1 or > 5)
            throw new ValidationException("Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(request.ReviewText) || request.ReviewText.Trim().Length > 2000)
            throw new ValidationException("ReviewText is required and cannot exceed 2000 characters.");

        var booking = await dbContext.BuddyBookings
            .FirstOrDefaultAsync(
                x => x.Id == request.BookingId &&
                     x.LocalBuddyUserId == buddyUserId,
                cancellationToken)
            ?? throw new NotFoundException("Completed buddy booking was not found.");
        if (booking.TravelerUserId != travelerUserId ||
            booking.Status != BuddyBookingStatus.Completed)
        {
            throw new ForbiddenException("Only the traveler from a completed buddy booking can review it.");
        }
        if (await dbContext.BuddyReviews.AnyAsync(x => x.BookingId == booking.Id, cancellationToken))
            throw new ValidationException("This buddy booking has already been reviewed.");

        var traveler = await dbContext.TravelerProfiles
            .AsNoTracking()
            .FirstAsync(x => x.UserId == travelerUserId, cancellationToken);
        var buddy = await GetBuddyAsync(buddyUserId, cancellationToken);
        var review = new BuddyReview
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            LocalBuddyUserId = buddyUserId,
            TravelerUserId = travelerUserId,
            Rating = request.Rating,
            ReviewText = request.ReviewText.Trim()
        };
        dbContext.BuddyReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);

        var aggregate = await dbContext.BuddyReviews
            .Where(x => x.LocalBuddyUserId == buddyUserId)
            .GroupBy(_ => 1)
            .Select(x => new { Count = x.Count(), Average = x.Average(r => r.Rating) })
            .SingleAsync(cancellationToken);
        buddy.ReviewsCount = aggregate.Count;
        buddy.Rating = Math.Round((decimal)aggregate.Average, 2);
        buddy.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapReview(review, traveler.DisplayName);
    }

    private Guid RequireRole(string role)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        if (!currentUser.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            throw new ForbiddenException($"The {role} role is required.");
        return currentUser.UserId.Value;
    }

    private void EnsureBuddyOwner(Guid buddyUserId)
    {
        var currentUserId = RequireRole(RoleNames.LocalBuddy);
        if (currentUserId != buddyUserId)
            throw new ForbiddenException("You cannot manage another local buddy's availability.");
    }

    private void EnsureBuddyOwnerOrAdmin(Guid buddyUserId)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        if (!currentUser.Roles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase) &&
            (!currentUser.Roles.Contains(RoleNames.LocalBuddy, StringComparer.OrdinalIgnoreCase) ||
             currentUser.UserId.Value != buddyUserId))
        {
            throw new ForbiddenException("You cannot manage another local buddy's requests.");
        }
    }

    private async Task<LocalBuddyProfile> GetBuddyAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.LocalBuddyProfiles.FirstOrDefaultAsync(
            x => x.UserId == userId,
            cancellationToken)
        ?? throw new NotFoundException("Local buddy profile not found.");

    private static void ValidateAvailability(BuddyAvailabilityRequest request)
    {
        if (request.StartTimeUtc.Kind == DateTimeKind.Unspecified ||
            request.EndTimeUtc.Kind == DateTimeKind.Unspecified)
            throw new ValidationException("Availability times must include a UTC offset.");
        if (request.StartTimeUtc <= DateTime.UtcNow)
            throw new ValidationException("Availability must start in the future.");
        if (request.EndTimeUtc <= request.StartTimeUtc)
            throw new ValidationException("Availability end time must be after the start time.");
        if (request.Price < 0)
            throw new ValidationException("Price cannot be negative.");
    }

    private static BuddyAvailabilityResponse MapAvailability(
        BuddyAvailability value,
        bool isBooked) => new()
    {
        Id = value.Id,
        LocalBuddyUserId = value.LocalBuddyUserId,
        StartTimeUtc = value.StartTimeUtc,
        EndTimeUtc = value.EndTimeUtc,
        Price = value.Price,
        IsActive = value.IsActive,
        IsBooked = isBooked
    };

    private async Task<List<BuddyBookingResponse>> MapBookingsAsync(
        List<BuddyBooking> bookings,
        CancellationToken cancellationToken)
    {
        var buddyNames = await GetBuddyNamesAsync(
            bookings.Select(x => x.LocalBuddyUserId),
            cancellationToken);
        var travelerNames = await GetTravelerNamesAsync(
            bookings.Select(x => x.TravelerUserId),
            cancellationToken);
        return bookings.Select(x => MapBooking(
            x,
            buddyNames.GetValueOrDefault(x.LocalBuddyUserId, "Local buddy"),
            travelerNames.GetValueOrDefault(x.TravelerUserId, "Traveler"))).ToList();
    }

    private async Task<BuddyBookingResponse> MapBookingAsync(
        BuddyBooking booking,
        CancellationToken cancellationToken)
    {
        var buddyName = await dbContext.LocalBuddyProfiles
            .Where(x => x.UserId == booking.LocalBuddyUserId)
            .Select(x => x.DisplayName)
            .SingleAsync(cancellationToken);
        var travelerName = await dbContext.TravelerProfiles
            .Where(x => x.UserId == booking.TravelerUserId)
            .Select(x => x.DisplayName)
            .SingleAsync(cancellationToken);
        return MapBooking(booking, buddyName, travelerName);
    }

    private static BuddyBookingResponse MapBooking(
        BuddyBooking value,
        string buddyName,
        string travelerName) => new()
    {
        Id = value.Id,
        AvailabilityId = value.AvailabilityId,
        LocalBuddyUserId = value.LocalBuddyUserId,
        BuddyName = buddyName,
        TravelerUserId = value.TravelerUserId,
        TravelerName = travelerName,
        StartTimeUtc = value.Availability.StartTimeUtc,
        EndTimeUtc = value.Availability.EndTimeUtc,
        TotalPrice = value.TotalPrice,
        Notes = value.Notes,
        Status = value.Status.ToString(),
        CreatedAtUtc = value.CreatedAtUtc,
        UpdatedAtUtc = value.UpdatedAtUtc
    };

    private async Task<Dictionary<Guid, string>> GetBuddyNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await dbContext.LocalBuddyProfiles
            .Where(x => ids.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.DisplayName, cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> GetTravelerNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        return await dbContext.TravelerProfiles
            .Where(x => ids.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, x => x.DisplayName, cancellationToken);
    }

    private static BuddyReviewResponse MapReview(BuddyReview value, string reviewerName) => new()
    {
        Id = value.Id,
        BookingId = value.BookingId,
        LocalBuddyUserId = value.LocalBuddyUserId,
        TravelerUserId = value.TravelerUserId,
        ReviewerName = reviewerName,
        Rating = value.Rating,
        ReviewText = value.ReviewText,
        CreatedAtUtc = value.CreatedAtUtc
    };
}
