using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Profiles.Domain.Enums;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Shared.Application.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Shared.Presentation.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IdentityAccessDbContext _identity;
    private readonly ProfilesDbContext _profiles;
    private readonly ExperiencesDbContext _experiences;
    private readonly StaysDbContext _stays;

    public AdminDashboardController(
        IdentityAccessDbContext identity,
        ProfilesDbContext profiles,
        ExperiencesDbContext experiences,
        StaysDbContext stays)
    {
        _identity = identity;
        _profiles = profiles;
        _experiences = experiences;
        _stays = stays;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var totalUsers = await _identity.Users.CountAsync(cancellationToken);
        var activeUsers = await _identity.Users.CountAsync(x => x.IsActive, cancellationToken);
        var pendingBuddies = await _profiles.LocalBuddyProfiles.CountAsync(
            x => x.VerificationStatus == VerificationStatus.Pending,
            cancellationToken);
        var approvedBuddies = await _profiles.LocalBuddyProfiles.CountAsync(
            x => x.VerificationStatus == VerificationStatus.Approved,
            cancellationToken);
        var pendingExperiences = await _experiences.Experiences.CountAsync(
            x => x.ModerationStatus == ExperienceModerationStatus.Pending,
            cancellationToken);
        var approvedExperiences = await _experiences.Experiences.CountAsync(
            x => x.ModerationStatus == ExperienceModerationStatus.Approved,
            cancellationToken);
        var activeStays = await _stays.Stays.CountAsync(x => x.IsActive, cancellationToken);
        var stayBookings = await _stays.StayBookings.CountAsync(cancellationToken);
        var experienceBookings = await _experiences.ExperienceBookings.CountAsync(cancellationToken);
        var stayValue = await _stays.StayBookings.SumAsync(x => (decimal?)x.TotalPrice, cancellationToken) ?? 0;
        var experienceValue = await _experiences.ExperienceBookings.SumAsync(
            x => (decimal?)x.TotalPrice,
            cancellationToken) ?? 0;
        var auditSince = DateTime.UtcNow.AddHours(-24);
        var recentAuditEvents = await _identity.AdminAuditEvents.CountAsync(
            x => x.CreatedAtUtc >= auditSince,
            cancellationToken);

        return Ok(new AdminDashboardResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            PendingBuddyVerifications = pendingBuddies,
            ApprovedBuddies = approvedBuddies,
            PendingExperiences = pendingExperiences,
            ApprovedExperiences = approvedExperiences,
            ActiveStays = activeStays,
            TotalBookings = stayBookings + experienceBookings,
            TotalBookingValue = stayValue + experienceValue,
            AuditEventsLast24Hours = recentAuditEvents
        });
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AdminAnalyticsResponse>> GetAnalytics(
        CancellationToken cancellationToken)
    {
        var usersByRole = await (
                from userRole in _identity.UserRoles
                join role in _identity.Roles on userRole.RoleId equals role.Id
                group userRole by role.Name into grouped
                select new { Role = grouped.Key!, Count = grouped.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count, cancellationToken);

        var stayStatuses = await _stays.StayBookings
            .AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var experienceStatuses = await _experiences.ExperienceBookings
            .AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var bookingsByStatus = stayStatuses
            .Select(x => x.ToString())
            .Concat(experienceStatuses.Select(x => x.ToString()))
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        var staysCount = await _stays.Stays.CountAsync(cancellationToken);
        var experiencesCount = await _experiences.Experiences.CountAsync(cancellationToken);
        var stayValue = await _stays.StayBookings.SumAsync(x => (decimal?)x.TotalPrice, cancellationToken) ?? 0;
        var experienceValue = await _experiences.ExperienceBookings.SumAsync(
            x => (decimal?)x.TotalPrice,
            cancellationToken) ?? 0;

        return Ok(new AdminAnalyticsResponse
        {
            UsersByRole = usersByRole,
            BookingsByStatus = bookingsByStatus,
            ListingsByType = new Dictionary<string, int>
            {
                ["Stays"] = staysCount,
                ["Experiences"] = experiencesCount
            },
            StayBookingValue = stayValue,
            ExperienceBookingValue = experienceValue
        });
    }
}
