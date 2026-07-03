using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.External.Groq;

public class GroqItineraryClient(
    HttpClient httpClient,
    IOptions<ItineraryPlanningOptions> options,
    ILogger<GroqItineraryClient> logger) : IItineraryGroqClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ItineraryPlanningOptions _options = options.Value;

    public async Task<ItineraryPlanPreferences?> ClassifyPlanAsync(
        string text,
        string? preferredLanguage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ResolveApiKey()))
        {
            logger.LogWarning("Groq API key is missing. Using local itinerary classification fallback.");
            return null;
        }

        try
        {
            var response = await SendChatAsync(
                BuildClassificationMessages(text, preferredLanguage),
                temperature: 0.1m,
                maxTokens: Math.Clamp(_options.GroqClassificationMaxTokens, 250, 1200),
                useJsonResponseFormat: true,
                cancellationToken);

            var content = response?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? null : ParseClassification(content);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Groq itinerary classification failed.");
            return null;
        }
    }

    private async Task<GroqChatResponse?> SendChatAsync(
        List<GroqMessage> messages,
        decimal temperature,
        int maxTokens,
        bool useJsonResponseFormat,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SendChatOnceAsync(messages, temperature, maxTokens, useJsonResponseFormat, cancellationToken);
        }
        catch (GroqHttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest &&
                  useJsonResponseFormat &&
                  _options.GroqRetryWithoutResponseFormatOnBadRequest)
        {
            logger.LogWarning(
                "Groq rejected itinerary JSON response_format. Retrying without response_format. Response: {GroqResponseBody}",
                Truncate(ex.ResponseBody, Math.Clamp(_options.GroqErrorBodyLogCharacters, 100, 2000)));

            return await SendChatOnceAsync(
                messages,
                temperature,
                maxTokens,
                useJsonResponseFormat: false,
                cancellationToken);
        }
    }

    private async Task<GroqChatResponse?> SendChatOnceAsync(
        List<GroqMessage> messages,
        decimal temperature,
        int maxTokens,
        bool useJsonResponseFormat,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUrl());
        request.Headers.Authorization = new("Bearer", ResolveApiKey());
        request.Content = JsonContent.Create(new GroqChatRequest
        {
            Model = ResolveModel(),
            Temperature = temperature,
            MaxTokens = maxTokens,
            ResponseFormat = useJsonResponseFormat ? new GroqResponseFormat { Type = "json_object" } : null,
            Messages = messages
        }, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GroqHttpRequestException(
                response.StatusCode,
                Truncate(responseBody, Math.Clamp(_options.GroqErrorBodyLogCharacters, 100, 2000)) ?? string.Empty);
        }

        return await response.Content.ReadFromJsonAsync<GroqChatResponse>(JsonOptions, cancellationToken);
    }

    private static List<GroqMessage> BuildClassificationMessages(string text, string? preferredLanguage)
    {
        return
        [
            new GroqMessage
            {
                Role = "system",
                Content = """
                Convert a user's tourism itinerary request into strict JSON only.
                Shape:
                {
                  "dayStartLocal":"10:00",
                  "dayEndLocal":"20:00",
                  "travelMode":"PublicTransit",
                  "fallbackTravelMode":"Walking",
                  "pace":"Balanced",
                  "categories":[{"category":"Historical","weight":70}],
                  "maxStops":5,
                  "includeMealBreaks":true,
                  "returnToStart":true,
                  "avoidLongWalking":false,
                  "crowdPreference":"Quiet",
                  "regionName":"Luxor",
                  "preferredLanguage":"ar",
                  "classificationConfidence":0.8,
                  "notes":""
                }
                Supported categories: Historical, Nature, Shopping, Nightlife, Dining.
                travelMode/fallbackTravelMode: Walking, Driving, Cycling, PublicTransit.
                pace: Relaxed, Balanced, Packed.
                crowdPreference: Quiet, Balanced, Lively, or null.
                Do not invent coordinates, exact places, or unavailable facts.
                regionName is a human-readable location from the request, never an administrative ID.
                Return Arabic preferredLanguage for Arabic text, otherwise English.
                """
            },
            new GroqMessage
            {
                Role = "user",
                Content = $"""
                Preferred language hint: {preferredLanguage ?? "auto"}
                User text:
                {text}
                """
            }
        ];
    }

    private static ItineraryPlanPreferences ParseClassification(string content)
    {
        using var document = JsonDocument.Parse(ExtractJsonObject(content));
        var root = document.RootElement;
        var preferences = new ItineraryPlanPreferences
        {
            DayStartLocal = ReadNullableTime(root, "dayStartLocal"),
            DayEndLocal = ReadNullableTime(root, "dayEndLocal"),
            MaxStops = ReadNullableInt(root, "maxStops"),
            IncludeMealBreaks = ReadNullableBool(root, "includeMealBreaks"),
            ReturnToStart = ReadNullableBool(root, "returnToStart"),
            AvoidLongWalking = ReadNullableBool(root, "avoidLongWalking"),
            PreferredLanguage = ReadString(root, "preferredLanguage") ?? "en",
            ClassificationConfidence = ReadNullableDouble(root, "classificationConfidence"),
            Notes = ReadString(root, "notes"),
            RegionName = ReadString(root, "regionName")
        };

        if (Enum.TryParse<ItineraryTravelMode>(ReadString(root, "travelMode"), true, out var travelMode))
        {
            preferences.TravelMode = travelMode;
        }

        if (Enum.TryParse<ItineraryTravelMode>(ReadString(root, "fallbackTravelMode"), true, out var fallbackMode))
        {
            preferences.FallbackTravelMode = fallbackMode;
        }

        if (Enum.TryParse<ItineraryPace>(ReadString(root, "pace"), true, out var pace))
        {
            preferences.Pace = pace;
        }

        if (Enum.TryParse<ExperienceCrowdPreference>(ReadString(root, "crowdPreference"), true, out var crowd))
        {
            preferences.CrowdPreference = crowd;
        }

        if (root.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in categories.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var category = ReadString(item, "category");
                if (string.IsNullOrWhiteSpace(category))
                {
                    continue;
                }

                preferences.Categories.Add(new ItineraryCategoryPreference
                {
                    Category = category,
                    Weight = ReadNullableDouble(item, "weight")
                });
            }
        }

        return preferences;
    }

    private string ResolveApiKey() =>
        FirstConfigured(
            _options.GroqApiKey,
            Environment.GetEnvironmentVariable("GROQ_API_KEY"));

    private string ResolveModel() =>
        FirstConfigured(
            _options.GroqModel,
            Environment.GetEnvironmentVariable("GROQ_MODEL"),
            "llama-3.1-8b-instant");

    private string BuildChatCompletionsUrl()
    {
        var configured = FirstConfigured(
            _options.GroqBaseUrl,
            Environment.GetEnvironmentVariable("GROQ_BASE_URL"),
            "https://api.groq.com/openai/v1");

        return configured.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? configured
            : $"{configured.TrimEnd('/')}/chat/completions";
    }

    private static string FirstConfigured(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new JsonException("Groq response did not contain a JSON object.");
        }

        return content[start..(end + 1)];
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? ReadNullableInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value) ? value : null;

    private static double? ReadNullableDouble(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetDouble(out var value) ? value : null;

    private static bool? ReadNullableBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : null;

    private static TimeOnly? ReadNullableTime(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String &&
        TimeOnly.TryParse(property.GetString(), out var value)
            ? value
            : null;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || maxLength <= 0)
        {
            return null;
        }

        var normalized = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength].Trim()}...";
    }

    private sealed class GroqChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<GroqMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("response_format")]
        public GroqResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class GroqMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GroqResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    private sealed class GroqChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice> Choices { get; set; } = [];
    }

    private sealed class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqMessage Message { get; set; } = new();
    }

    private sealed class GroqHttpRequestException(HttpStatusCode statusCode, string responseBody)
        : HttpRequestException($"Groq request failed with status code {(int)statusCode}.", null, statusCode)
    {
        public string ResponseBody { get; } = responseBody;
    }
}
