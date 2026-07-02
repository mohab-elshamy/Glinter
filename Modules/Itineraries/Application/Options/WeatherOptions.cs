namespace Glinter.Modules.Itineraries.Application.Options;

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";
    public string Provider { get; set; } = "OpenMeteo";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.open-meteo.com";
    public int TimeoutSeconds { get; set; } = 10;
    public int CacheMinutes { get; set; } = 15;
    public int MaxForecastDays { get; set; } = 16;
}
