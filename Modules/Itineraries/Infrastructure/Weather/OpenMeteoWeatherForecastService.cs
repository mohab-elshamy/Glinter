using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.Weather;

public sealed class OpenMeteoWeatherForecastService(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<WeatherOptions> options,
    ILogger<OpenMeteoWeatherForecastService> logger) : IWeatherForecastService
{
    private readonly WeatherOptions _options = options.Value;

    public async Task<WeatherForecastResponse> GetForecastAsync(
        WeatherForecastRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var cacheKey =
            $"weather:{request.Latitude:F3}:{request.Longitude:F3}:{request.StartDate}:{request.EndDate}";
        if (cache.TryGetValue(cacheKey, out WeatherForecastResponse? cached) &&
            cached is not null)
        {
            return cached;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastSupportedDate = today.AddDays(Math.Max(1, _options.MaxForecastDays) - 1);
        if (request.EndDate < today || request.StartDate > lastSupportedDate)
        {
            return Unavailable(request.Location, "The requested dates are outside the forecast window.");
        }

        var start = request.StartDate < today ? today : request.StartDate;
        var end = request.EndDate > lastSupportedDate ? lastSupportedDate : request.EndDate;
        var url =
            $"/v1/forecast?latitude={request.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&longitude={request.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}" +
            "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max,relative_humidity_2m_mean" +
            "&timezone=auto";

        try
        {
            var provider = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(
                url,
                cancellationToken);
            if (provider?.Daily?.Time is null)
            {
                return Unavailable(request.Location, "The weather provider returned no forecast data.");
            }

            var result = new WeatherForecastResponse
            {
                Location = request.Location,
                IsAvailable = true,
                ProviderDataTimestampUtc = DateTime.UtcNow,
                Days = Enumerable.Range(0, provider.Daily.Time.Count)
                    .Select(index => MapDay(provider.Daily, index))
                    .ToList()
            };
            cache.Set(
                cacheKey,
                result,
                TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Weather provider timed out.");
            return Unavailable(request.Location, "The weather provider timed out.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Weather provider request failed.");
            return Unavailable(request.Location, "Weather is temporarily unavailable.");
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Weather provider returned an invalid response.");
            return Unavailable(request.Location, "The weather provider returned invalid data.");
        }
    }

    private static DailyWeatherResponse MapDay(OpenMeteoDaily daily, int index)
    {
        var precipitation = At(daily.PrecipitationProbability, index);
        var code = At(daily.WeatherCode, index);
        var min = At(daily.TemperatureMin, index);
        var max = At(daily.TemperatureMax, index);
        var wind = At(daily.WindSpeed, index);
        var humidity = At(daily.Humidity, index);
        return new DailyWeatherResponse
        {
            Date = DateOnly.Parse(daily.Time[index]),
            TemperatureMinC = min,
            TemperatureMaxC = max,
            Condition = Describe(code),
            PrecipitationProbabilityPercent = precipitation is null
                ? null
                : (int)Math.Round(precipitation.Value),
            WindSpeedKph = wind,
            HumidityPercent = humidity is null ? null : (int)Math.Round(humidity.Value),
            Advice = Advice(precipitation, max, wind)
        };
    }

    private static string Advice(double? precipitation, double? max, double? wind)
    {
        if (precipitation >= 60) return "Carry rain protection and keep an indoor alternative.";
        if (max >= 35) return "Plan outdoor activities early and carry water.";
        if (wind >= 40) return "Expect strong wind and secure loose belongings.";
        return "Conditions do not require special travel precautions.";
    }

    private static string Describe(double? value) => value switch
    {
        0 => "Clear",
        1 or 2 or 3 => "Partly cloudy",
        45 or 48 => "Fog",
        >= 51 and <= 67 => "Rain",
        >= 71 and <= 77 => "Snow",
        >= 80 and <= 82 => "Rain showers",
        >= 95 => "Thunderstorm",
        _ => "Unknown"
    };

    private static double? At(IReadOnlyList<double?>? values, int index) =>
        values is not null && index < values.Count ? values[index] : null;

    private static void Validate(WeatherForecastRequest request)
    {
        if (request.Latitude is < -90 or > 90 ||
            request.Longitude is < -180 or > 180)
            throw new ArgumentException("Coordinates are outside their valid range.");
        if (request.EndDate < request.StartDate)
            throw new ArgumentException("End date cannot be before start date.");
        if (request.EndDate.DayNumber - request.StartDate.DayNumber > 30)
            throw new ArgumentException("Weather range cannot exceed 31 days.");
    }

    private static WeatherForecastResponse Unavailable(string? location, string reason) =>
        new()
        {
            Location = location,
            IsAvailable = false,
            UnavailableReason = reason
        };

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public OpenMeteoDaily? Daily { get; set; }
    }

    private sealed class OpenMeteoDaily
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = [];
        [JsonPropertyName("weather_code")]
        public List<double?> WeatherCode { get; set; } = [];
        [JsonPropertyName("temperature_2m_max")]
        public List<double?> TemperatureMax { get; set; } = [];
        [JsonPropertyName("temperature_2m_min")]
        public List<double?> TemperatureMin { get; set; } = [];
        [JsonPropertyName("precipitation_probability_max")]
        public List<double?> PrecipitationProbability { get; set; } = [];
        [JsonPropertyName("wind_speed_10m_max")]
        public List<double?> WindSpeed { get; set; } = [];
        [JsonPropertyName("relative_humidity_2m_mean")]
        public List<double?> Humidity { get; set; } = [];
    }
}
