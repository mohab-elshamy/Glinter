using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Profiles.Domain.Enums;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Shared.Application.Dashboards;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Shared.Infrastructure.Dashboards;

public sealed class DashboardService
{
    private readonly IdentityAccessDbContext _identity;
    private readonly ProfilesDbContext _profiles;
    private readonly StaysDbContext _stays;
    private readonly ExperiencesDbContext _experiences;
    private readonly CommunicationDbContext _communication;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(
        IdentityAccessDbContext identity,
        ProfilesDbContext profiles,
        StaysDbContext stays,
        ExperiencesDbContext experiences,
        CommunicationDbContext communication,
        ICurrentUserService currentUser)
    {
        _identity = identity;
        _profiles = profiles;
        _stays = stays;
        _experiences = experiences;
        _communication = communication;
        _currentUser = currentUser;
    }

    public async Task<AdminDashboardDto> GetAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);
        var last24Hours = now.AddHours(-24);

        return new AdminDashboardDto
        {
            TotalUsers = await _identity.Users.CountAsync(cancellationToken),
            ActiveUsers = await _identity.Users.CountAsync(
                x => x.IsActive,
                cancellationToken),
            NewUsersLast30Days = await _identity.Users.CountAsync(
                x => x.CreatedAtUtc >= last30Days,
                cancellationToken),
            TotalStays = await _stays.Stays.CountAsync(cancellationToken),
            ActiveStays = await _stays.Stays.CountAsync(
                x => x.IsActive,
                cancellationToken),
            StayBookings = await _stays.StayBookings.CountAsync(cancellationToken),
            TotalExperiences = await _experiences.Experiences.CountAsync(cancellationToken),
            ApprovedExperiences = await _experiences.Experiences.CountAsync(
                x => x.ApprovalStatus == ExperienceApprovalStatus.Approved,
                cancellationToken),
            PendingExperiences = await _experiences.Experiences.CountAsync(
                x => x.ApprovalStatus == ExperienceApprovalStatus.Pending,
                cancellationToken),
            RejectedExperiences = await _experiences.Experiences.CountAsync(
                x => x.ApprovalStatus == ExperienceApprovalStatus.Rejected,
                cancellationToken),
            ExperienceBookings =
                await _experiences.ExperienceBookings.CountAsync(cancellationToken),
            PendingLocalBuddyVerifications =
                await _profiles.LocalBuddyProfiles.CountAsync(
                    x => x.VerificationStatus == VerificationStatus.Pending,
                    cancellationToken),
            ChatMessagesLast24Hours = await _communication.ChatMessages.CountAsync(
                x => x.SentAtUtc >= last24Hours,
                cancellationToken),
            UnreadNotifications = await _communication.Notifications.CountAsync(
                x => x.ReadAtUtc == null,
                cancellationToken),
            GeneratedAtUtc = now
        };
    }

    public async Task<HotelOwnerDashboardDto> GetHotelOwnerAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var profileId = await _profiles.HotelOwnerProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Hotel owner profile was not found.");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var next30Days = today.AddDays(30);
        var stays = _stays.Stays.Where(x => x.OwnerProfileId == profileId);
        var bookings = _stays.StayBookings.Where(
            booking => stays.Any(stay => stay.Id == booking.StayId));
        var reviews = _stays.StayReviews.Where(
            review => stays.Any(stay => stay.Id == review.StayId));

        var revenue = await bookings
            .Where(x => x.Status != "Cancelled")
            .Join(
                _stays.Stays,
                booking => booking.StayId,
                stay => stay.Id,
                (booking, stay) => new { booking.TotalPrice, stay.Currency })
            .GroupBy(x => x.Currency)
            .Select(x => new RevenueByCurrencyDto
            {
                Currency = x.Key,
                Amount = x.Sum(value => value.TotalPrice)
            })
            .OrderBy(x => x.Currency)
            .ToListAsync(cancellationToken);

        return new HotelOwnerDashboardDto
        {
            ProfileId = profileId,
            TotalStays = await stays.CountAsync(cancellationToken),
            ActiveStays = await stays.CountAsync(x => x.IsActive, cancellationToken),
            TotalBookings = await bookings.CountAsync(cancellationToken),
            ActiveBookings = await bookings.CountAsync(
                x => x.Status != "Cancelled" && x.Status != "Completed",
                cancellationToken),
            CancelledBookings = await bookings.CountAsync(
                x => x.Status == "Cancelled",
                cancellationToken),
            UpcomingCheckInsNext30Days = await bookings.CountAsync(
                x => x.Status != "Cancelled" &&
                     x.CheckInDate >= today &&
                     x.CheckInDate <= next30Days,
                cancellationToken),
            ReviewCount = await reviews.CountAsync(cancellationToken),
            AverageRating = await reviews
                .Select(x => (double?)x.Rating)
                .AverageAsync(cancellationToken) ?? 0,
            Revenue = revenue,
            GeneratedAtUtc = now
        };
    }

    public async Task<ExperienceProviderDashboardDto> GetExperienceProviderAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var profileId = await _profiles.ExperienceProviderProfiles
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Experience provider profile was not found.");

        var now = DateTime.UtcNow;
        var experiences = _experiences.Experiences
            .Where(x => x.ProviderProfileId == profileId);
        var bookings = _experiences.ExperienceBookings.Where(
            booking => experiences.Any(experience => experience.Id == booking.ExperienceId));
        var reviews = _experiences.ExperienceReviews.Where(
            review => experiences.Any(experience => experience.Id == review.ExperienceId));

        var revenue = await bookings
            .Where(x => x.Status != ExperienceBookingStatus.Cancelled)
            .Join(
                _experiences.Experiences,
                booking => booking.ExperienceId,
                experience => experience.Id,
                (booking, experience) => new
                {
                    booking.TotalPrice,
                    experience.Currency
                })
            .GroupBy(x => x.Currency)
            .Select(x => new RevenueByCurrencyDto
            {
                Currency = x.Key,
                Amount = x.Sum(value => value.TotalPrice)
            })
            .OrderBy(x => x.Currency)
            .ToListAsync(cancellationToken);

        return new ExperienceProviderDashboardDto
        {
            ProfileId = profileId,
            TotalExperiences = await experiences.CountAsync(cancellationToken),
            ActiveExperiences = await experiences.CountAsync(
                x => x.IsActive,
                cancellationToken),
            PendingExperiences = await experiences.CountAsync(
                x => x.ApprovalStatus == ExperienceApprovalStatus.Pending,
                cancellationToken),
            ApprovedExperiences = await experiences.CountAsync(
                x => x.ApprovalStatus == ExperienceApprovalStatus.Approved,
                cancellationToken),
            RejectedExperiences = await experiences.CountAsync(
                x => x.ApprovalStatus == ExperienceApprovalStatus.Rejected,
                cancellationToken),
            UpcomingAvailabilitySlots =
                await _experiences.ExperienceAvailability.CountAsync(
                    x => x.IsActive &&
                         x.StartTimeUtc > now &&
                         experiences.Any(experience => experience.Id == x.ExperienceId),
                    cancellationToken),
            TotalBookings = await bookings.CountAsync(cancellationToken),
            ConfirmedBookings = await bookings.CountAsync(
                x => x.Status == ExperienceBookingStatus.Confirmed,
                cancellationToken),
            CompletedBookings = await bookings.CountAsync(
                x => x.Status == ExperienceBookingStatus.Completed,
                cancellationToken),
            CancelledBookings = await bookings.CountAsync(
                x => x.Status == ExperienceBookingStatus.Cancelled,
                cancellationToken),
            GuestsBooked = await bookings
                .Where(x => x.Status != ExperienceBookingStatus.Cancelled)
                .SumAsync(x => (int?)x.GuestsCount, cancellationToken) ?? 0,
            ReviewCount = await reviews.CountAsync(cancellationToken),
            AverageRating = await reviews
                .Select(x => (double?)x.Rating)
                .AverageAsync(cancellationToken) ?? 0,
            Revenue = revenue,
            GeneratedAtUtc = now
        };
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new AuthenticationException("User is not authenticated.");
        return _currentUser.UserId.Value;
    }
}
