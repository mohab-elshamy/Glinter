using Glinter.Modules.Buddy.Application.Abstractions;
using Glinter.Modules.Buddy.Application.Requests.Dtos;
using Glinter.Modules.Buddy.Application.Reviews.Dtos;
using Glinter.Modules.Buddy.Domain.Entities;
using Glinter.Modules.Buddy.Domain.Enums;
using Glinter.Modules.Buddy.Domain;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Domain.Enums;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Glinter.Modules.Buddy.Application.Requests;

public sealed class BuddyRequestService(
    IBuddyDbContext dbContext,
    IBuddyProfileReader profiles,
    ICurrentUserService currentUser,
    CreateNotificationHandler notifications)
{
    private static readonly BuddyBookingStatus[] BlockingStatuses =
        [BuddyBookingStatus.Pending, BuddyBookingStatus.Accepted];

    public async Task<List<BuddyAvailabilityResponse>> GetAvailabilityAsync(
        Guid buddyUserId,
        bool manage,
        CancellationToken cancellationToken)
    {
        var buddy = await RequireBuddyAsync(buddyUserId, cancellationToken);
        if (manage)
            EnsureBuddyOwner(buddyUserId);
        else if (!buddy.IsApproved)
            throw new NotFoundException("Local buddy profile not found.");

        var query = dbContext.BuddyAvailabilities
            .AsNoTracking()
            .Where(x => x.LocalBuddyUserId == buddyUserId);
        if (!manage)
            query = query.Where(x => x.IsActive && x.EndTimeUtc > DateTime.UtcNow);

        return await query.OrderBy(x => x.StartTimeUtc)
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
        await RequireBuddyAsync(buddyUserId, cancellationToken);
        ValidateAvailability(request);
        if (await dbContext.BuddyAvailabilities.AnyAsync(
                x => x.LocalBuddyUserId == buddyUserId &&
                     x.IsActive &&
                     request.StartTimeUtc < x.EndTimeUtc &&
                     request.EndTimeUtc > x.StartTimeUtc,
                cancellationToken))
            throw new ValidationException("This availability overlaps an active slot.");

        var entity = new BuddyAvailability
        {
            Id = Guid.NewGuid(),
            LocalBuddyUserId = buddyUserId,
            StartTimeUtc = request.StartTimeUtc.ToUniversalTime(),
            EndTimeUtc = request.EndTimeUtc.ToUniversalTime(),
            Price = request.Price
        };
        dbContext.BuddyAvailabilities.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAvailability(entity, false);
    }

    public async Task<BuddyAvailabilityResponse> UpdateAvailabilityAsync(
        Guid availabilityId,
        BuddyAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAvailability(request);
        var entity = await dbContext.BuddyAvailabilities
            .Include(x => x.Bookings)
            .SingleOrDefaultAsync(x => x.Id == availabilityId, cancellationToken)
            ?? throw new NotFoundException("Buddy availability was not found.");
        EnsureBuddyOwner(entity.LocalBuddyUserId);
        if (entity.Bookings.Any(x => BlockingStatuses.Contains(x.Status)))
            throw new ConflictException("A requested availability cannot be rescheduled.");
        if (await dbContext.BuddyAvailabilities.AnyAsync(
                x => x.Id != availabilityId &&
                     x.LocalBuddyUserId == entity.LocalBuddyUserId &&
                     x.IsActive &&
                     request.StartTimeUtc < x.EndTimeUtc &&
                     request.EndTimeUtc > x.StartTimeUtc,
                cancellationToken))
            throw new ValidationException("This availability overlaps an active slot.");

        entity.StartTimeUtc = request.StartTimeUtc.ToUniversalTime();
        entity.EndTimeUtc = request.EndTimeUtc.ToUniversalTime();
        entity.Price = request.Price;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAvailability(entity, false);
    }

    public async Task<BuddyAvailabilityResponse> SetAvailabilityActiveAsync(
        Guid availabilityId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.BuddyAvailabilities
            .Include(x => x.Bookings)
            .SingleOrDefaultAsync(x => x.Id == availabilityId, cancellationToken)
            ?? throw new NotFoundException("Buddy availability was not found.");
        EnsureBuddyOwner(entity.LocalBuddyUserId);
        var isBooked = entity.Bookings.Any(x => BlockingStatuses.Contains(x.Status));
        if (!isActive && isBooked)
            throw new ConflictException("A requested availability cannot be deactivated.");
        entity.IsActive = isActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAvailability(entity, isBooked);
    }

    public async Task<BuddyRequestResponse> CreateRequestAsync(
        CreateBuddyRequest request,
        CancellationToken cancellationToken)
    {
        var travelerUserId = RequireRole(RoleNames.Traveler);
        if (request.LocalBuddyUserId == Guid.Empty)
            throw new ValidationException("LocalBuddyUserId is required.");
        if (request.AvailabilityId == Guid.Empty)
            throw new ValidationException("AvailabilityId is required.");
        var notes = request.Notes?.Trim();
        if (notes?.Length > 1000)
            throw new ValidationException("Notes cannot exceed 1000 characters.");
        if (travelerUserId == request.LocalBuddyUserId)
            throw new ValidationException("You cannot send a buddy request to yourself.");

        var traveler = await profiles.GetTravelerAsync(
            travelerUserId, cancellationToken)
            ?? throw new ValidationException(
                "Create your traveler profile before requesting a buddy.");
        var buddy = await RequireBuddyAsync(
            request.LocalBuddyUserId, cancellationToken);
        if (!buddy.IsApproved)
            throw new NotFoundException("Local buddy profile not found.");

        var availability = await dbContext.BuddyAvailabilities
            .SingleOrDefaultAsync(
                x => x.Id == request.AvailabilityId &&
                     x.LocalBuddyUserId == request.LocalBuddyUserId,
                cancellationToken)
            ?? throw new NotFoundException("Buddy availability was not found.");
        if (!availability.IsActive ||
            availability.StartTimeUtc <= DateTime.UtcNow ||
            availability.EndTimeUtc <= availability.StartTimeUtc)
            throw new ValidationException(
                "This buddy availability is no longer bookable.");
        if (await dbContext.BuddyBookings.AnyAsync(
                x => x.AvailabilityId == availability.Id &&
                     BlockingStatuses.Contains(x.Status),
                cancellationToken))
            throw new ValidationException(
                "This buddy availability has already been requested.");

        var booking = new BuddyBooking
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            LocalBuddyUserId = buddy.UserId,
            TravelerUserId = traveler.UserId,
            TotalPrice = availability.Price,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
        };
        dbContext.BuddyBookings.Add(booking);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            throw new ValidationException(
                "This buddy availability has already been requested.");
        }

        booking.Availability = availability;
        await NotifyAsync(
            buddy.UserId,
            "New buddy request",
            $"{traveler.DisplayName} requested one of your available times.",
            "/profile/me?tab=buddy-schedule",
            booking.Id,
            cancellationToken);
        return MapRequest(
            booking, buddy.DisplayName, traveler.DisplayName, false);
    }

    public Task<BuddyRequestResponse> CreateLegacyRequestAsync(
        Guid buddyUserId,
        CreateBuddyBookingRequest request,
        CancellationToken cancellationToken) =>
        CreateRequestAsync(
            new CreateBuddyRequest
            {
                LocalBuddyUserId = buddyUserId,
                AvailabilityId = request.AvailabilityId,
                Notes = request.Notes
            },
            cancellationToken);

    public async Task<List<BuddyRequestResponse>> GetMineAsync(
        CancellationToken cancellationToken)
    {
        var userId = RequireRole(RoleNames.Traveler);
        return await GetRequestsAsync(
            x => x.TravelerUserId == userId, cancellationToken);
    }

    public async Task<List<BuddyRequestResponse>> GetIncomingAsync(
        CancellationToken cancellationToken)
    {
        var userId = RequireRole(RoleNames.LocalBuddy);
        return await GetRequestsAsync(
            x => x.LocalBuddyUserId == userId, cancellationToken);
    }

    public async Task<List<BuddyRequestResponse>> GetBuddyRequestsAsync(
        Guid buddyUserId,
        CancellationToken cancellationToken)
    {
        EnsureBuddyOwnerOrAdmin(buddyUserId);
        return await GetRequestsAsync(
            x => x.LocalBuddyUserId == buddyUserId, cancellationToken);
    }

    public async Task<BuddyRequestResponse> GetRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        var booking = await LoadBookingAsync(requestId, cancellationToken);
        var isAdmin = HasRole(RoleNames.Admin);
        if (!isAdmin &&
            booking.TravelerUserId != userId &&
            booking.LocalBuddyUserId != userId)
            throw new ForbiddenException("You cannot view this buddy request.");
        return await MapRequestAsync(booking, cancellationToken);
    }

    public Task<BuddyRequestResponse> AcceptAsync(
        Guid requestId,
        CancellationToken cancellationToken) =>
        DecideAsync(requestId, BuddyBookingStatus.Accepted, cancellationToken);

    public Task<BuddyRequestResponse> RejectAsync(
        Guid requestId,
        CancellationToken cancellationToken) =>
        DecideAsync(requestId, BuddyBookingStatus.Rejected, cancellationToken);

    public async Task<BuddyRequestResponse> UpdateLegacyStatusAsync(
        Guid requestId,
        UpdateBuddyBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BuddyBookingStatus>(request.Status, true, out var status))
            throw new ValidationException("Status is invalid.");
        if (status == BuddyBookingStatus.Accepted)
            return HasRole(RoleNames.Admin)
                ? await DecideAsync(
                    requestId, status, cancellationToken, allowAdmin: true)
                : await AcceptAsync(requestId, cancellationToken);
        if (status == BuddyBookingStatus.Rejected)
            return HasRole(RoleNames.Admin)
                ? await DecideAsync(
                    requestId, status, cancellationToken, allowAdmin: true)
                : await RejectAsync(requestId, cancellationToken);
        if (status != BuddyBookingStatus.Completed)
            throw new ValidationException(
                "Status must be Accepted, Rejected, or Completed.");

        var booking = await LoadBookingAsync(requestId, cancellationToken);
        EnsureBuddyOwnerOrAdmin(booking.LocalBuddyUserId);
        if (booking.Status != BuddyBookingStatus.Accepted)
            throw new ConflictException(
                "Only an accepted request can be marked completed.");
        booking.Status = BuddyBookingStatus.Completed;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapRequestAsync(booking, cancellationToken);
    }

    public async Task<BuddyRequestResponse> CancelAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var travelerUserId = RequireRole(RoleNames.Traveler);
        var booking = await LoadBookingAsync(requestId, cancellationToken);
        if (booking.TravelerUserId != travelerUserId)
            throw new ForbiddenException(
                "You cannot cancel another traveler's buddy request.");
        if (booking.Status is not (
                BuddyBookingStatus.Pending or BuddyBookingStatus.Accepted))
            throw new ConflictException(
                "This buddy request can no longer be cancelled.");
        if (booking.Availability.StartTimeUtc <= DateTime.UtcNow)
            throw new ConflictException(
                "A buddy request cannot be cancelled after its start time.");

        booking.Status = BuddyBookingStatus.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAsync(
            booking.LocalBuddyUserId,
            "Buddy request cancelled",
            "A traveler cancelled their local buddy request.",
            "/profile/me?tab=buddy-schedule",
            booking.Id,
            cancellationToken);
        return await MapRequestAsync(booking, cancellationToken);
    }

    public async Task<List<BuddyReviewResponse>> GetReviewsAsync(
        Guid buddyUserId,
        CancellationToken cancellationToken)
    {
        var buddy = await RequireBuddyAsync(buddyUserId, cancellationToken);
        if (!buddy.IsApproved)
            throw new NotFoundException("Local buddy profile not found.");
        var reviews = await dbContext.BuddyReviews.AsNoTracking()
            .Where(x => x.LocalBuddyUserId == buddyUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var names = await profiles.GetTravelerDisplayNamesAsync(
            reviews.Select(x => x.TravelerUserId), cancellationToken);
        return reviews.Select(x => MapReview(
            x, names.GetValueOrDefault(x.TravelerUserId, "Traveler"))).ToList();
    }

    public async Task<BuddyReviewSummaryResponse> GetReviewSummaryAsync(
        Guid buddyUserId,
        CancellationToken cancellationToken)
    {
        var buddy = await RequireBuddyAsync(buddyUserId, cancellationToken);
        if (!buddy.IsApproved)
            throw new NotFoundException("Local buddy profile not found.");
        return await dbContext.BuddyReviews.AsNoTracking()
            .Where(x => x.LocalBuddyUserId == buddyUserId)
            .GroupBy(_ => 1)
            .Select(x => new BuddyReviewSummaryResponse
            {
                ReviewsCount = x.Count(),
                AverageRating = Math.Round(x.Average(r => (decimal)r.Rating), 2)
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? new BuddyReviewSummaryResponse();
    }

    public async Task<BuddyReviewResponse> CreateReviewAsync(
        Guid requestId,
        CreateBuddyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var travelerUserId = RequireRole(RoleNames.Traveler);
        BuddyRequestPolicy.ValidateRating(request.Rating);
        var text = request.ReviewText.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
            throw new ValidationException(
                "ReviewText is required and cannot exceed 2000 characters.");

        var booking = await LoadBookingAsync(requestId, cancellationToken);
        if (booking.TravelerUserId != travelerUserId)
            throw new ForbiddenException(
                "Only the traveler who created this request can review it.");
        BuddyRequestPolicy.EnsureCanReview(
            booking.Status,
            booking.Availability.EndTimeUtc,
            booking.Review is not null,
            DateTime.UtcNow);

        var traveler = await profiles.GetTravelerAsync(
            travelerUserId, cancellationToken)
            ?? throw new ValidationException("Traveler profile not found.");
        var review = new BuddyReview
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            LocalBuddyUserId = booking.LocalBuddyUserId,
            TravelerUserId = travelerUserId,
            Rating = request.Rating,
            ReviewText = text
        };
        dbContext.BuddyReviews.Add(review);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException(
                "This buddy request has already been reviewed.");
        }

        var summary = await GetReviewSummaryInternalAsync(
            booking.LocalBuddyUserId, cancellationToken);
        await profiles.UpdateLocalBuddyReviewSummaryAsync(
            booking.LocalBuddyUserId,
            summary.AverageRating,
            summary.ReviewsCount,
            cancellationToken);
        await NotifyAsync(
            booking.LocalBuddyUserId,
            "New buddy review",
            $"{traveler.DisplayName} reviewed your local buddy session.",
            $"/profile/{booking.LocalBuddyUserId}",
            review.Id,
            cancellationToken,
            "BuddyReview");
        return MapReview(review, traveler.DisplayName);
    }

    public Task<BuddyReviewResponse> CreateLegacyReviewAsync(
        Guid buddyUserId,
        LegacyCreateBuddyReviewRequest request,
        CancellationToken cancellationToken) =>
        CreateReviewForBuddyAsync(buddyUserId, request, cancellationToken);

    private async Task<BuddyReviewResponse> CreateReviewForBuddyAsync(
        Guid buddyUserId,
        LegacyCreateBuddyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await dbContext.BuddyBookings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Buddy request was not found.");
        if (booking.LocalBuddyUserId != buddyUserId)
            throw new NotFoundException("Buddy request was not found.");
        return await CreateReviewAsync(
            request.BookingId,
            new CreateBuddyReviewRequest
            {
                Rating = request.Rating,
                ReviewText = request.ReviewText
            },
            cancellationToken);
    }

    private async Task<BuddyRequestResponse> DecideAsync(
        Guid requestId,
        BuddyBookingStatus decision,
        CancellationToken cancellationToken,
        bool allowAdmin = false)
    {
        var actorUserId = RequireAuthenticated();
        var isAdmin = allowAdmin && HasRole(RoleNames.Admin);
        if (!isAdmin && !HasRole(RoleNames.LocalBuddy))
            throw new ForbiddenException(
                $"The {RoleNames.LocalBuddy} role is required.");
        var booking = await LoadBookingAsync(requestId, cancellationToken);
        if (!isAdmin && booking.LocalBuddyUserId != actorUserId)
            throw new ForbiddenException(
                "You cannot respond to another local buddy's request.");
        BuddyRequestPolicy.EnsureCanDecide(booking.Status);

        booking.Status = decision;
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await NotifyAsync(
            booking.TravelerUserId,
            $"Buddy request {decision.ToString().ToLowerInvariant()}",
            $"Your local buddy request was {decision.ToString().ToLowerInvariant()}.",
            "/profile/me?tab=bookings",
            booking.Id,
            cancellationToken);
        return await MapRequestAsync(booking, cancellationToken);
    }

    private async Task<List<BuddyRequestResponse>> GetRequestsAsync(
        System.Linq.Expressions.Expression<Func<BuddyBooking, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var requests = await dbContext.BuddyBookings.AsNoTracking()
            .Include(x => x.Availability)
            .Include(x => x.Review)
            .Where(predicate)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var buddyNames = await profiles.GetLocalBuddyDisplayNamesAsync(
            requests.Select(x => x.LocalBuddyUserId), cancellationToken);
        var travelerNames = await profiles.GetTravelerDisplayNamesAsync(
            requests.Select(x => x.TravelerUserId), cancellationToken);
        return requests.Select(x => MapRequest(
            x,
            buddyNames.GetValueOrDefault(x.LocalBuddyUserId, "Local buddy"),
            travelerNames.GetValueOrDefault(x.TravelerUserId, "Traveler"),
            x.Review is not null)).ToList();
    }

    private async Task<BuddyRequestResponse> MapRequestAsync(
        BuddyBooking booking,
        CancellationToken cancellationToken)
    {
        var buddyNames = await profiles.GetLocalBuddyDisplayNamesAsync(
            [booking.LocalBuddyUserId], cancellationToken);
        var travelerNames = await profiles.GetTravelerDisplayNamesAsync(
            [booking.TravelerUserId], cancellationToken);
        return MapRequest(
            booking,
            buddyNames.GetValueOrDefault(
                booking.LocalBuddyUserId, "Local buddy"),
            travelerNames.GetValueOrDefault(
                booking.TravelerUserId, "Traveler"),
            booking.Review is not null);
    }

    private async Task<BuddyBooking> LoadBookingAsync(
        Guid requestId,
        CancellationToken cancellationToken) =>
        await dbContext.BuddyBookings
            .Include(x => x.Availability)
            .Include(x => x.Review)
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken)
        ?? throw new NotFoundException("Buddy request was not found.");

    private static BuddyRequestResponse MapRequest(
        BuddyBooking value,
        string buddyName,
        string travelerName,
        bool hasReview)
    {
        var now = DateTime.UtcNow;
        var accepted = value.Status is
            BuddyBookingStatus.Accepted or BuddyBookingStatus.Completed;
        return new BuddyRequestResponse
        {
            Id = value.Id,
            AvailabilityId = value.AvailabilityId,
            LocalBuddyUserId = value.LocalBuddyUserId,
            LocalBuddyDisplayName = buddyName,
            TravelerUserId = value.TravelerUserId,
            TravelerDisplayName = travelerName,
            StartTimeUtc = value.Availability.StartTimeUtc,
            EndTimeUtc = value.Availability.EndTimeUtc,
            TotalPrice = value.TotalPrice,
            Notes = value.Notes,
            Status = value.Status.ToString(),
            CreatedAtUtc = value.CreatedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
            RespondedAtUtc = value.Status == BuddyBookingStatus.Pending
                ? null
                : value.UpdatedAtUtc,
            CanCancel = value.Status is
                    BuddyBookingStatus.Pending or BuddyBookingStatus.Accepted &&
                value.Availability.StartTimeUtc > now,
            CanReview = accepted &&
                value.Availability.EndTimeUtc <= now &&
                !hasReview,
            HasReview = hasReview
        };
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

    private static BuddyReviewResponse MapReview(
        BuddyReview value,
        string travelerName) => new()
    {
        Id = value.Id,
        RequestId = value.BookingId,
        LocalBuddyUserId = value.LocalBuddyUserId,
        TravelerUserId = value.TravelerUserId,
        TravelerDisplayName = travelerName,
        Rating = value.Rating,
        ReviewText = value.ReviewText,
        CreatedAtUtc = value.CreatedAtUtc
    };

    private async Task<BuddyReviewSummaryResponse> GetReviewSummaryInternalAsync(
        Guid buddyUserId,
        CancellationToken cancellationToken) =>
        await dbContext.BuddyReviews.AsNoTracking()
            .Where(x => x.LocalBuddyUserId == buddyUserId)
            .GroupBy(_ => 1)
            .Select(x => new BuddyReviewSummaryResponse
            {
                ReviewsCount = x.Count(),
                AverageRating = Math.Round(x.Average(r => (decimal)r.Rating), 2)
            })
            .SingleAsync(cancellationToken);

    private async Task<LocalBuddyProfileSummary> RequireBuddyAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await profiles.GetLocalBuddyAsync(userId, cancellationToken)
        ?? throw new NotFoundException("Local buddy profile not found.");

    private static void ValidateAvailability(BuddyAvailabilityRequest request)
    {
        BuddyRequestPolicy.ValidateAvailability(
            request.StartTimeUtc,
            request.EndTimeUtc,
            request.Price,
            DateTime.UtcNow);
    }

    private Guid RequireAuthenticated()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        return currentUser.UserId.Value;
    }

    private Guid RequireRole(string role)
    {
        var userId = RequireAuthenticated();
        if (!HasRole(role))
            throw new ForbiddenException($"The {role} role is required.");
        return userId;
    }

    private bool HasRole(string role) =>
        currentUser.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    private void EnsureBuddyOwner(Guid buddyUserId)
    {
        if (RequireRole(RoleNames.LocalBuddy) != buddyUserId)
            throw new ForbiddenException(
                "You cannot manage another local buddy's availability.");
    }

    private void EnsureBuddyOwnerOrAdmin(Guid buddyUserId)
    {
        var userId = RequireAuthenticated();
        if (!HasRole(RoleNames.Admin) &&
            (!HasRole(RoleNames.LocalBuddy) || userId != buddyUserId))
            throw new ForbiddenException(
                "You cannot manage another local buddy's requests.");
    }

    private async Task NotifyAsync(
        Guid userId,
        string title,
        string body,
        string link,
        Guid entityId,
        CancellationToken cancellationToken,
        string entityType = "BuddyRequest") =>
        await notifications.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = userId,
                Type = NotificationType.Booking,
                Title = title,
                Body = body,
                LinkUrl = link,
                SourceModule = "Buddy",
                SourceEntityType = entityType,
                SourceEntityId = entityId
            },
            cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
