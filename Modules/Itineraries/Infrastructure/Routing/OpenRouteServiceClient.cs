using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.Routing;

public class OpenRouteServiceClient(
    HttpClient httpClient,
    IOptions<ItineraryPlanningOptions> options,
    ILogger<OpenRouteServiceClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ItineraryPlanningOptions _options = options.Value;

    public async Task<ItineraryRouteResult?> TryGetRouteAsync(
        ItineraryRouteRequest request,
        CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("OpenRouteService API key is missing. Falling back to route estimate.");
            return null;
        }

        var profile = request.Mode switch
        {
            ItineraryTravelMode.Driving => "driving-car",
            ItineraryTravelMode.Cycling => "cycling-regular",
            ItineraryTravelMode.Walking => "foot-walking",
            _ => null
        };

        if (profile is null)
        {
            return null;
        }

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ResolveBaseUrl().TrimEnd('/')}/v2/directions/{profile}/json");

        httpRequest.Headers.TryAddWithoutValidation("Authorization", apiKey);
        httpRequest.Content = JsonContent.Create(new OpenRouteServiceDirectionsRequest
        {
            Coordinates =
            [
                [request.From.Longitude, request.From.Latitude],
                [request.To.Longitude, request.To.Latitude]
            ],
            Instructions = true
        }, options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "OpenRouteService route request failed with {StatusCode}: {Body}",
                (int)response.StatusCode,
                Truncate(body, 500));
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<OpenRouteServiceDirectionsResponse>(
            JsonOptions,
            cancellationToken);

        var route = payload?.Routes.FirstOrDefault();
        if (route is null)
        {
            return null;
        }

        var steps = route.Segments
            .SelectMany(x => x.Steps)
            .Select(x => x.Instruction)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Take(8)
            .ToList();

        return new ItineraryRouteResult
        {
            Mode = request.Mode,
            Provider = ItineraryRouteProvider.OpenRouteService,
            DistanceKm = Math.Round(route.Summary.Distance / 1000, 2, MidpointRounding.AwayFromZero),
            DurationMinutes = Math.Max(1, (int)Math.Ceiling(route.Summary.Duration / 60)),
            Geometry = RouteGeometryBuilder.FromEncodedPolyline(route.Geometry),
            Steps = steps
        };
    }

    private string ResolveApiKey() =>
        FirstConfigured(_options.OpenRouteServiceApiKey, Environment.GetEnvironmentVariable("OPENROUTESERVICE_API_KEY"));

    private string ResolveBaseUrl() =>
        FirstConfigured(_options.OpenRouteServiceBaseUrl, Environment.GetEnvironmentVariable("OPENROUTESERVICE_BASE_URL"), "https://api.openrouteservice.org");

    private static string FirstConfigured(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : $"{value[..maxLength]}...";
    }

    private sealed class OpenRouteServiceDirectionsRequest
    {
        [JsonPropertyName("coordinates")]
        public List<double[]> Coordinates { get; set; } = [];

        [JsonPropertyName("instructions")]
        public bool Instructions { get; set; }
    }

    private sealed class OpenRouteServiceDirectionsResponse
    {
        [JsonPropertyName("routes")]
        public List<OpenRouteServiceRoute> Routes { get; set; } = [];
    }

    private sealed class OpenRouteServiceRoute
    {
        [JsonPropertyName("summary")]
        public OpenRouteServiceSummary Summary { get; set; } = new();

        [JsonPropertyName("geometry")]
        public string? Geometry { get; set; }

        [JsonPropertyName("segments")]
        public List<OpenRouteServiceSegment> Segments { get; set; } = [];
    }

    private sealed class OpenRouteServiceSummary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    private sealed class OpenRouteServiceSegment
    {
        [JsonPropertyName("steps")]
        public List<OpenRouteServiceStep> Steps { get; set; } = [];
    }

    private sealed class OpenRouteServiceStep
    {
        [JsonPropertyName("instruction")]
        public string? Instruction { get; set; }
    }
}
