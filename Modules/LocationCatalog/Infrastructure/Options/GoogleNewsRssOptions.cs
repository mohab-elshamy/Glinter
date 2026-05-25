namespace Glinter.Modules.LocationCatalog.Infrastructure.Options;

public sealed class GoogleNewsRssOptions
{
    public string SearchUrl { get; set; } = "https://news.google.com/rss/search";

    public string EnglishHl { get; set; } = "en-US";
    public string EnglishGl { get; set; } = "EG";
    public string EnglishCeid { get; set; } = "EG:en";

    public string ArabicHl { get; set; } = "ar";
    public string ArabicGl { get; set; } = "EG";
    public string ArabicCeid { get; set; } = "EG:ar";
}