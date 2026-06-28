namespace Glinter.Modules.SafetyIndex.Application.Dtos;

public class SafetyScoreDto
{
    public int? Score { get; set; }

    public string? GeneralSafetyDescription { get; set; }

    public string? TrendingEventDescription { get; set; }

    public DateTimeOffset? CalculatedAtUtc { get; set; }

    public int NewsItemCount { get; set; }
}

public class Adm2SafetyIndexResponseDto
{
    public int Adm2Gid { get; set; }

    public int Adm1Gid { get; set; }

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public SafetyScoreDto WeeklyScore { get; set; } = new();

    public SafetyScoreDto HistoricalScore { get; set; } = new();
}

public class SafetyScoreEstimateDto
{
    public int Score { get; set; }

    public string GeneralSafetyDescription { get; set; } = string.Empty;

    public string TrendingEventDescription { get; set; } = string.Empty;
}
