namespace Glinter.Modules.SafetyIndex.Application.Options;

public sealed class SafetyIndexOptions
{
    public const string SectionName = "SafetyIndex";

    public string GroqApiKey { get; set; } =
        string.Empty;

    public string GroqModel { get; set; } =
        "qwen/qwen3-32b";

    public string GroqChatCompletionsUrl { get; set; } =
        "https://api.groq.com/openai/v1/chat/completions";

    public string GoogleNewsRssBaseUrl { get; set; } =
        "https://news.google.com/rss/search";

    public string GoogleNewsHl { get; set; } =
        "ar";

    public string GoogleNewsGl { get; set; } =
        "EG";

    public string GoogleNewsCeid { get; set; } =
        "EG:ar";

    public string QueryCountryHint { get; set; } =
        "مصر";

    public int NewsLookbackDays { get; set; } =
        7;

    /*
     * Keep this disabled normally.
     *
     * Enable it only when you intentionally want to build
     * the historical safety dataset.
     */
    public bool RunInitialHistoricalCollectionOnStartup
    {
        get;
        set;
    } = false;

    /*
     * External API background jobs should be opt-in.
     *
     * Enable this in the deployed/demo environment after
     * configuring the SafetyIndex Groq API key.
     */
    public bool EnableWeeklyService
    {
        get;
        set;
    } = false;

    public List<int> LimitAdm2Gids { get; set; } =
        [];

    public string NewsHistoryDirectory { get; set; } =
        "Data/SafetyIndex/NewsHistory";

    public int WeeklyRefreshIntervalHours { get; set; } =
        168;

    public int HostedServiceStartupDelaySeconds
    {
        get;
        set;
    } = 15;

    public int RssRequestTimeoutSeconds { get; set; } =
        30;

    public int GroqRequestTimeoutSeconds { get; set; } =
        60;

    public int GroqRequestsPerMinute { get; set; } =
        5;

    public int MaxTitlesPerPrompt { get; set; } =
        120;

    public int GroqMaxTitleCharacters { get; set; } =
        180;

    public int GroqMaxPromptTitleCharacters
    {
        get;
        set;
    } = 8000;
}