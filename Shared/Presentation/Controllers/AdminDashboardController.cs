using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Shared.Application.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Shared.Presentation.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin")]
public sealed class AdminDashboardController(
    IIdentityAdminReadService identity,
    IProfilesAdminReadService profiles,
    IExperiencesAdminReadService experiences,
    IStaysAdminReadService stays) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var identitySnapshot = await identity.GetSnapshotAsync(
            DateTime.UtcNow.AddHours(-24),
            cancellationToken);
        var profileSnapshot = await profiles.GetSnapshotAsync(cancellationToken);
        var experienceSnapshot = await experiences.GetSnapshotAsync(cancellationToken);
        var staySnapshot = await stays.GetSnapshotAsync(cancellationToken);

        return Ok(new AdminDashboardResponse
        {
            TotalUsers = identitySnapshot.TotalUsers,
            ActiveUsers = identitySnapshot.ActiveUsers,
            PendingBuddyVerifications = profileSnapshot.PendingBuddyVerifications,
            ApprovedBuddies = profileSnapshot.ApprovedBuddies,
            PendingExperiences = experienceSnapshot.PendingExperiences,
            ApprovedExperiences = experienceSnapshot.ApprovedExperiences,
            ActiveStays = staySnapshot.ActiveStays,
            TotalBookings = staySnapshot.BookingCount + experienceSnapshot.BookingCount,
            TotalBookingValue = staySnapshot.BookingValue + experienceSnapshot.BookingValue,
            AuditEventsLast24Hours = identitySnapshot.RecentAuditEvents
        });
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AdminAnalyticsResponse>> GetAnalytics(
        CancellationToken cancellationToken)
    {
        var identitySnapshot = await identity.GetSnapshotAsync(
            DateTime.UtcNow.AddHours(-24),
            cancellationToken);
        var experienceSnapshot = await experiences.GetSnapshotAsync(cancellationToken);
        var staySnapshot = await stays.GetSnapshotAsync(cancellationToken);
        var bookingsByStatus = staySnapshot.BookingStatuses
            .Concat(experienceSnapshot.BookingStatuses)
            .GroupBy(status => status)
            .ToDictionary(group => group.Key, group => group.Count());

        return Ok(new AdminAnalyticsResponse
        {
            UsersByRole = identitySnapshot.UsersByRole.ToDictionary(),
            BookingsByStatus = bookingsByStatus,
            ListingsByType = new Dictionary<string, int>
            {
                ["Stays"] = staySnapshot.TotalStays,
                ["Experiences"] = experienceSnapshot.TotalExperiences
            },
            StayBookingValue = staySnapshot.BookingValue,
            ExperienceBookingValue = experienceSnapshot.BookingValue
        });
    }
}
