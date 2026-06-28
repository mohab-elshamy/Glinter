namespace Glinter.Modules.SafetyIndex.Domain.Entities;

public class SafetyIndexResult
{
    public int Id { get; set; }

    public int Adm2Gid { get; set; }

    public int? WeeklyScore { get; set; }

    public string? WeeklyGeneralSafetyDescription { get; set; }

    public string? WeeklyTrendingEventDescription { get; set; }

    public DateTimeOffset? WeeklyCalculatedAtUtc { get; set; }

    public DateTimeOffset? WeeklyNewsFromUtc { get; set; }

    public DateTimeOffset? WeeklyNewsToUtc { get; set; }

    public int WeeklyNewsItemCount { get; set; }

    public int? HistoricalScore { get; set; }

    public string? HistoricalGeneralSafetyDescription { get; set; }

    public string? HistoricalTrendingEventDescription { get; set; }

    public DateTimeOffset? HistoricalCalculatedAtUtc { get; set; }

    public int HistoricalNewsItemCount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
