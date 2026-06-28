namespace Glinter.Shared.Infrastructure.Cleanup;

public sealed class DataCleanupOptions
{
    public const string SectionName = "Cleanup";
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 500;
    public int MaxBatchesPerRun { get; set; } = 10;
    public int RevokedTokenRetentionDays { get; set; } = 7;
    public int RefreshTokenRetentionDays { get; set; } = 7;
    public int MfaChallengeRetentionDays { get; set; } = 1;
    public int ReadNotificationRetentionDays { get; set; } = 90;
    public int UnreadNotificationRetentionDays { get; set; } = 365;
}
