namespace Glinter.Shared.Application.Analytics;

public sealed class DailyBusinessActivityDto
{
    public DateOnly Date { get; set; }
    public int NewUsers { get; set; }
    public int NewStays { get; set; }
    public int NewExperiences { get; set; }
    public int StayBookings { get; set; }
    public int ExperienceBookings { get; set; }
}

public sealed class AdminAnalyticsDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int NewUsers { get; set; }
    public int NewStays { get; set; }
    public int NewExperiences { get; set; }
    public int StayBookings { get; set; }
    public int ExperienceBookings { get; set; }
    public List<Glinter.Shared.Application.Dashboards.GrossBookingValueByCurrencyDto>
        GrossBookingValue { get; set; } = [];
    public List<DailyBusinessActivityDto> DailyActivity { get; set; } = [];
}
