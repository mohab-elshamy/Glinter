namespace Glinter.Modules.LocationCatalog.Domain.Entities;

public class DistrictSafetySignal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DistrictId { get; set; }

    public District District { get; set; } = null!;

    public string SourceType { get; set; } = string.Empty;
    // News, TravelerReport, AdminReport, BuddyFeedback, MockNews

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string RiskCategory { get; set; } = string.Empty;
    // none, theft, harassment, scam, protest, traffic, violence, accident, other

    public string Severity { get; set; } = string.Empty;
    // none, low, medium, high, critical

    public double Confidence { get; set; }
    // 0 to 1

    public double SentimentScore { get; set; }
    // -1 to 1

    public bool IsSafetyRelevant { get; set; }

    public string AiSummary { get; set; } = string.Empty;

    public string RawAiJson { get; set; } = string.Empty;

    public DateTime PublishedAtUtc { get; set; }

    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}