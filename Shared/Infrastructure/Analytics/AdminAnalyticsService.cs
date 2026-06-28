using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Shared.Application.Analytics;
using Glinter.Shared.Application.Dashboards;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Shared.Infrastructure.Analytics;

public sealed class AdminAnalyticsService
{
    private readonly IdentityAccessDbContext _identity;
    private readonly StaysDbContext _stays;
    private readonly ExperiencesDbContext _experiences;

    public AdminAnalyticsService(
        IdentityAccessDbContext identity,
        StaysDbContext stays,
        ExperiencesDbContext experiences)
    {
        _identity = identity;
        _stays = stays;
        _experiences = experiences;
    }

    public async Task<AdminAnalyticsDto> GetAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var resolvedTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedFrom = from ?? resolvedTo.AddDays(-29);
        if (resolvedFrom > resolvedTo)
            throw new ValidationException("From must be on or before To.");
        if (resolvedTo.DayNumber - resolvedFrom.DayNumber > 365)
            throw new ValidationException("Analytics range cannot exceed 366 days.");

        var fromUtc = DateTime.SpecifyKind(
            resolvedFrom.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        var toExclusiveUtc = DateTime.SpecifyKind(
            resolvedTo.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var users = await _identity.Users
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toExclusiveUtc)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(x => new DateCount(x.Key, x.Count()))
            .ToListAsync(cancellationToken);
        var stays = await _stays.Stays
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toExclusiveUtc)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(x => new DateCount(x.Key, x.Count()))
            .ToListAsync(cancellationToken);
        var experiences = await _experiences.Experiences
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toExclusiveUtc)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(x => new DateCount(x.Key, x.Count()))
            .ToListAsync(cancellationToken);
        var stayBookings = await _stays.StayBookings
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toExclusiveUtc)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(x => new DateCount(x.Key, x.Count()))
            .ToListAsync(cancellationToken);
        var experienceBookings = await _experiences.ExperienceBookings
            .Where(x => x.CreatedAtUtc >= fromUtc && x.CreatedAtUtc < toExclusiveUtc)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .Select(x => new DateCount(x.Key, x.Count()))
            .ToListAsync(cancellationToken);

        var stayRevenue = await _stays.StayBookings
            .Where(x => x.CreatedAtUtc >= fromUtc &&
                        x.CreatedAtUtc < toExclusiveUtc &&
                        x.Status != "Cancelled")
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
            .ToListAsync(cancellationToken);
        var experienceRevenue = await _experiences.ExperienceBookings
            .Where(x => x.CreatedAtUtc >= fromUtc &&
                        x.CreatedAtUtc < toExclusiveUtc &&
                        x.Status != ExperienceBookingStatus.Cancelled)
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
            .ToListAsync(cancellationToken);

        var userLookup = users.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var stayLookup = stays.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var experienceLookup =
            experiences.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var stayBookingLookup =
            stayBookings.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var experienceBookingLookup =
            experienceBookings.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);

        var daily = new List<DailyBusinessActivityDto>();
        for (var date = resolvedFrom; date <= resolvedTo; date = date.AddDays(1))
        {
            daily.Add(new DailyBusinessActivityDto
            {
                Date = date,
                NewUsers = userLookup.GetValueOrDefault(date),
                NewStays = stayLookup.GetValueOrDefault(date),
                NewExperiences = experienceLookup.GetValueOrDefault(date),
                StayBookings = stayBookingLookup.GetValueOrDefault(date),
                ExperienceBookings = experienceBookingLookup.GetValueOrDefault(date)
            });
        }

        var revenue = stayRevenue
            .Concat(experienceRevenue)
            .GroupBy(x => x.Currency, StringComparer.OrdinalIgnoreCase)
            .Select(x => new RevenueByCurrencyDto
            {
                Currency = x.Key.ToUpperInvariant(),
                Amount = x.Sum(value => value.Amount)
            })
            .OrderBy(x => x.Currency)
            .ToList();

        return new AdminAnalyticsDto
        {
            From = resolvedFrom,
            To = resolvedTo,
            NewUsers = users.Sum(x => x.Count),
            NewStays = stays.Sum(x => x.Count),
            NewExperiences = experiences.Sum(x => x.Count),
            StayBookings = stayBookings.Sum(x => x.Count),
            ExperienceBookings = experienceBookings.Sum(x => x.Count),
            Revenue = revenue,
            DailyActivity = daily
        };
    }

    private sealed record DateCount(DateTime Date, int Count);
}
