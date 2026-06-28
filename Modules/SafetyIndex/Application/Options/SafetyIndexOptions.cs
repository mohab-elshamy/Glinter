namespace Glinter.Modules.SafetyIndex.Application.Options;

public class SafetyIndexOptions
{
    public const string SectionName = "SafetyIndex";

    public string GroqApiKey { get; set; } = string.Empty;

    public string GroqModel { get; set; } = "qwen/qwen3-32b";

    public string GroqChatCompletionsUrl { get; set; } = "https://api.groq.com/openai/v1/chat/completions";

    public string GoogleNewsRssBaseUrl { get; set; } = "https://news.google.com/rss/search";

    public string GoogleNewsHl { get; set; } = "ar";

    public string GoogleNewsGl { get; set; } = "EG";

    public string GoogleNewsCeid { get; set; } = "EG:ar";

    public string QueryCountryHint { get; set; } = "مصر";

    public int NewsLookbackDays { get; set; } = 7;

    public bool RunInitialHistoricalCollectionOnStartup { get; set; }

    public bool EnableWeeklyService { get; set; } = true;

    public List<int> LimitAdm2Gids { get; set; } = [];

    public string NewsHistoryDirectory { get; set; } = "Data/SafetyIndex/NewsHistory";

    public int WeeklyRefreshIntervalHours { get; set; } = 168;

    public int HostedServiceStartupDelaySeconds { get; set; } = 15;

    public int RssRequestTimeoutSeconds { get; set; } = 30;

    public int GroqRequestTimeoutSeconds { get; set; } = 60;

    public int GroqRequestsPerMinute { get; set; } = 10;

    public int MaxTitlesPerPrompt { get; set; } = 120;

    public int GroqMaxTitleCharacters { get; set; } = 180;

    public int GroqMaxPromptTitleCharacters { get; set; } = 8000;
}
