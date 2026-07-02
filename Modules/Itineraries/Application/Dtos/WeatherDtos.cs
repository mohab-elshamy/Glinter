namespace Glinter.Modules.Itineraries.Application.Dtos;

public sealed class WeatherForecastRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Location { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public sealed class WeatherForecastResponse
{
    public string? Location { get; set; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime? ProviderDataTimestampUtc { get; set; }
    public List<DailyWeatherResponse> Days { get; set; } = [];
}

public sealed class DailyWeatherResponse
{
    public DateOnly Date { get; set; }
    public double? TemperatureMinC { get; set; }
    public double? TemperatureMaxC { get; set; }
    public string? Condition { get; set; }
    public int? PrecipitationProbabilityPercent { get; set; }
    public double? WindSpeedKph { get; set; }
    public int? HumidityPercent { get; set; }
    public string? Advice { get; set; }
}
