using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.Routing;

public class OpenTripPlannerClient(
    HttpClient httpClient,
    IOptions<ItineraryPlanningOptions> options,
    ILogger<OpenTripPlannerClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string PlanQuery = """
        query PlanItinerary(
          $origin: PlanLabeledLocationInput!,
          $destination: PlanLabeledLocationInput!,
          $dateTime: PlanDateTimeInput!,
          $modes: PlanModesInput!
        ) {
          planConnection(
            first: 1,
            origin: $origin,
            destination: $destination,
            dateTime: $dateTime,
            modes: $modes
          ) {
            routingErrors {
              code
              description
            }
            edges {
              node {
                duration
                walkDistance
                legs {
                  mode
                  distance
                  duration
                  transitLeg
                  from {
                    name
                    lat
                    lon
                  }
                  to {
                    name
                    lat
                    lon
                  }
                  legGeometry {
                    points
                    length
                  }
                }
              }
            }
          }
        }
        """;
    private readonly ItineraryPlanningOptions _options = options.Value;

    public async Task<ItineraryRouteResult?> TryGetRouteAsync(
        ItineraryRouteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildGraphQlUrl());
            httpRequest.Content = JsonContent.Create(new OtpGraphQlRequest
            {
                Query = PlanQuery,
                Variables = BuildVariables(request)
            }, options: JsonOptions);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "OpenTripPlanner GraphQL request failed with {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    Truncate(body, 500));
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenTripPlannerGraphQlResponse>(
                JsonOptions,
                cancellationToken);

            if (payload?.Errors.Count > 0)
            {
                logger.LogWarning(
                    "OpenTripPlanner GraphQL returned errors: {Errors}",
                    string.Join(" | ", payload.Errors.Select(x => x.Message).Take(3)));
            }

            var planConnection = payload?.Data?.PlanConnection;
            var itinerary = planConnection?.Edges.FirstOrDefault()?.Node;
            if (itinerary is null)
            {
                if (planConnection?.RoutingErrors.Count > 0)
                {
                    logger.LogInformation(
                        "OpenTripPlanner found no itinerary. Routing errors: {RoutingErrors}",
                        string.Join(" | ", planConnection.RoutingErrors.Select(x => $"{x.Code}: {x.Description}")));
                }

                return null;
            }

            var geometry = RouteGeometryBuilder.CombineEncodedPolylines(
                itinerary.Legs.Select(x => x.LegGeometry?.Points));

            var steps = itinerary.Legs
                .Select(x => $"{x.Mode}: {x.From?.Name ?? "origin"} to {x.To?.Name ?? "destination"}")
                .Take(8)
                .ToList();
            var distanceMeters = itinerary.Legs.Sum(x => x.Distance) > 0
                ? itinerary.Legs.Sum(x => x.Distance)
                : itinerary.WalkDistance;
            var hasTransitLeg = itinerary.Legs.Any(x => x.TransitLeg);
            var warnings = planConnection?.RoutingErrors
                .Select(x => string.IsNullOrWhiteSpace(x.Description)
                    ? $"OpenTripPlanner routing note: {x.Code}"
                    : $"OpenTripPlanner routing note: {x.Description}")
                .Distinct()
                .ToList() ?? [];

            return new ItineraryRouteResult
            {
                Mode = hasTransitLeg ? ItineraryTravelMode.PublicTransit : ParseLegMode(itinerary.Legs.FirstOrDefault()?.Mode),
                Provider = ItineraryRouteProvider.OpenTripPlanner,
                DistanceKm = Math.Round(Math.Max(0, distanceMeters) / 1000, 2, MidpointRounding.AwayFromZero),
                DurationMinutes = Math.Max(1, (int)Math.Ceiling(itinerary.Duration / 60)),
                Geometry = geometry ?? RouteGeometryBuilder.FromStraightLine(
                    request.From.Latitude,
                    request.From.Longitude,
                    request.To.Latitude,
                    request.To.Longitude),
                Steps = steps,
                Warnings = warnings
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "OpenTripPlanner GraphQL request failed.");
            return null;
        }
    }

    private OtpPlanVariables BuildVariables(ItineraryRouteRequest request)
    {
        var dateTime = new DateTimeOffset(
            request.DepartureLocal,
            TimeZoneInfo.Local.GetUtcOffset(request.DepartureLocal));

        return new OtpPlanVariables
        {
            Origin = BuildLocation(request.From),
            Destination = BuildLocation(request.To),
            DateTime = new OtpPlanDateTime
            {
                EarliestDeparture = dateTime.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)
            },
            Modes = request.AvoidLongWalking
                ? BuildTransitOnlyModes()
                : BuildTransitWithDirectWalkModes()
        };
    }

    private static OtpLabeledLocation BuildLocation(Application.Dtos.ItineraryPointRequest point) =>
        new()
        {
            Label = point.Label,
            Location = new OtpLocation
            {
                Coordinate = new OtpCoordinate
                {
                    Latitude = point.Latitude,
                    Longitude = point.Longitude
                }
            }
        };

    private static OtpModes BuildTransitWithDirectWalkModes() =>
        new()
        {
            Direct = ["WALK"],
            Transit = BuildTransitModes()
        };

    private static OtpModes BuildTransitOnlyModes() =>
        new()
        {
            TransitOnly = true,
            Transit = BuildTransitModes()
        };

    private static OtpTransitModes BuildTransitModes() =>
        new()
        {
            Access = ["WALK"],
            Egress = ["WALK"],
            Transfer = ["WALK"],
            Transit =
            [
                new OtpTransitModePreference { Mode = "BUS" },
                new OtpTransitModePreference { Mode = "RAIL" },
                new OtpTransitModePreference { Mode = "SUBWAY" },
                new OtpTransitModePreference { Mode = "TRAM" },
                new OtpTransitModePreference { Mode = "FERRY" }
            ]
        };

    private static ItineraryTravelMode ParseLegMode(string? mode) =>
        mode?.ToUpperInvariant() switch
        {
            "CAR" => ItineraryTravelMode.Driving,
            "BICYCLE" => ItineraryTravelMode.Cycling,
            "SCOOTER" => ItineraryTravelMode.Cycling,
            "WALK" => ItineraryTravelMode.Walking,
            _ => ItineraryTravelMode.PublicTransit
        };

    private string BuildGraphQlUrl()
    {
        var baseUrl = ResolveBaseUrl().TrimEnd('/');
        var path = FirstConfigured(_options.OpenTripPlannerGraphQlPath, "/otp/gtfs/v1");
        return $"{baseUrl}/{path.TrimStart('/')}";
    }

    private string ResolveBaseUrl() =>
        FirstConfigured(_options.OpenTripPlannerBaseUrl, Environment.GetEnvironmentVariable("OPENTRIPPLANNER_BASE_URL"), "http://localhost:8040");

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

    private sealed class OtpGraphQlRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public OtpPlanVariables Variables { get; set; } = new();
    }

    private sealed class OtpPlanVariables
    {
        [JsonPropertyName("origin")]
        public OtpLabeledLocation Origin { get; set; } = new();

        [JsonPropertyName("destination")]
        public OtpLabeledLocation Destination { get; set; } = new();

        [JsonPropertyName("dateTime")]
        public OtpPlanDateTime DateTime { get; set; } = new();

        [JsonPropertyName("modes")]
        public OtpModes Modes { get; set; } = new();
    }

    private sealed class OtpLabeledLocation
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("location")]
        public OtpLocation Location { get; set; } = new();
    }

    private sealed class OtpLocation
    {
        [JsonPropertyName("coordinate")]
        public OtpCoordinate Coordinate { get; set; } = new();
    }

    private sealed class OtpCoordinate
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class OtpPlanDateTime
    {
        [JsonPropertyName("earliestDeparture")]
        public string EarliestDeparture { get; set; } = string.Empty;
    }

    private sealed class OtpModes
    {
        [JsonPropertyName("direct")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Direct { get; set; }

        [JsonPropertyName("transitOnly")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TransitOnly { get; set; }

        [JsonPropertyName("transit")]
        public OtpTransitModes Transit { get; set; } = new();
    }

    private sealed class OtpTransitModes
    {
        [JsonPropertyName("access")]
        public string[] Access { get; set; } = [];

        [JsonPropertyName("egress")]
        public string[] Egress { get; set; } = [];

        [JsonPropertyName("transfer")]
        public string[] Transfer { get; set; } = [];

        [JsonPropertyName("transit")]
        public OtpTransitModePreference[] Transit { get; set; } = [];
    }

    private sealed class OtpTransitModePreference
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;
    }

    private sealed class OpenTripPlannerGraphQlResponse
    {
        [JsonPropertyName("data")]
        public OpenTripPlannerData? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<OpenTripPlannerGraphQlError> Errors { get; set; } = [];
    }

    private sealed class OpenTripPlannerGraphQlError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    private sealed class OpenTripPlannerData
    {
        [JsonPropertyName("planConnection")]
        public OpenTripPlannerPlanConnection? PlanConnection { get; set; }
    }

    private sealed class OpenTripPlannerPlanConnection
    {
        [JsonPropertyName("routingErrors")]
        public List<OpenTripPlannerRoutingError> RoutingErrors { get; set; } = [];

        [JsonPropertyName("edges")]
        public List<OpenTripPlannerPlanEdge> Edges { get; set; } = [];
    }

    private sealed class OpenTripPlannerRoutingError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private sealed class OpenTripPlannerPlanEdge
    {
        [JsonPropertyName("node")]
        public OpenTripPlannerItinerary? Node { get; set; }
    }

    private sealed class OpenTripPlannerItinerary
    {
        [JsonPropertyName("duration")]
        public double Duration { get; set; }

        [JsonPropertyName("walkDistance")]
        public double WalkDistance { get; set; }

        [JsonPropertyName("legs")]
        public List<OpenTripPlannerLeg> Legs { get; set; } = [];
    }

    private sealed class OpenTripPlannerLeg
    {
        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("transitLeg")]
        public bool TransitLeg { get; set; }

        [JsonPropertyName("from")]
        public OpenTripPlannerPlace? From { get; set; }

        [JsonPropertyName("to")]
        public OpenTripPlannerPlace? To { get; set; }

        [JsonPropertyName("legGeometry")]
        public OpenTripPlannerGeometry? LegGeometry { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }

    private sealed class OpenTripPlannerPlace
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class OpenTripPlannerGeometry
    {
        [JsonPropertyName("points")]
        public string? Points { get; set; }
    }
}
