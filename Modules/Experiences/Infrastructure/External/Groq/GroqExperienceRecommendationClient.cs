using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Options;
using Glinter.Modules.Experiences.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Experiences.Infrastructure.External.Groq;

public class GroqExperienceRecommendationClient(
    HttpClient httpClient,
    IOptions<ExperienceRecommendationOptions> options,
    ILogger<GroqExperienceRecommendationClient> logger) : IExperienceRecommendationGroqClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ExperienceRecommendationOptions _options = options.Value;

    public async Task<ExperienceRecommendationPreferences?> ClassifyAsync(
        string text,
        string? preferredLanguage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ResolveApiKey()))
        {
            logger.LogWarning("Groq API key is missing. Using local experience recommendation classifier.");
            return null;
        }

        try
        {
            var response = await SendChatAsync(
                BuildClassificationMessages(text, preferredLanguage),
                temperature: 0.1m,
                maxTokens: Math.Clamp(_options.GroqClassificationMaxTokens, 200, 1200),
                useJsonResponseFormat: true,
                cancellationToken);

            var content = response?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? null : ParseClassification(content);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Groq experience recommendation classification failed.");
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<int, ExperienceRecommendationExplanationResponse>?> GenerateExplanationsAsync(
        ExperienceRecommendationPreferences preferences,
        IReadOnlyList<ExperienceRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ResolveApiKey()) || rankedItems.Count == 0)
        {
            return null;
        }

        try
        {
            var items = rankedItems
                .Take(Math.Clamp(_options.GroqMaxExplanationItems, 1, 20))
                .ToList();

            var response = await SendChatAsync(
                BuildExplanationMessages(preferences, items, compact: false),
                temperature: 0.2m,
                maxTokens: Math.Clamp(_options.GroqExplanationMaxTokens, 300, 1200),
                useJsonResponseFormat: true,
                cancellationToken);

            var content = response?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? null : ParseExplanations(content, rankedItems);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            logger.LogWarning(ex, "Groq experience explanation payload was too large. Retrying compact payload.");
            return await RetryCompactExplanationsAsync(preferences, rankedItems, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Groq experience recommendation explanations failed.");
            return null;
        }
    }

    private async Task<IReadOnlyDictionary<int, ExperienceRecommendationExplanationResponse>?> RetryCompactExplanationsAsync(
        ExperienceRecommendationPreferences preferences,
        IReadOnlyList<ExperienceRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = rankedItems
                .Take(Math.Clamp(_options.GroqRetryMaxExplanationItems, 1, 8))
                .ToList();

            var response = await SendChatAsync(
                BuildExplanationMessages(preferences, items, compact: true),
                temperature: 0.2m,
                maxTokens: Math.Clamp(_options.GroqExplanationMaxTokens, 300, 900),
                useJsonResponseFormat: true,
                cancellationToken);

            var content = response?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? null : ParseExplanations(content, rankedItems);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Compact Groq experience recommendation explanations failed.");
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
                "Groq rejected experience recommendation JSON response_format. Retrying without response_format. Response: {GroqResponseBody}",
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
                Convert tourism experience recommendation text into strict JSON only.
                Shape:
                {
                  "categories":[{"category":"Historical","weight":1}],
                  "crowdPreference":"Quiet",
                  "regionName":"Cairo",
                  "visitAtLocal": null,
                  "forItinerary": true,
                  "preferredLanguage":"ar",
                  "classificationConfidence":0.8,
                  "notes":""
                }
                Supported categories: Historical, Nature, Shopping, Nightlife, Dining.
                crowdPreference can be Quiet, Balanced, Lively, or null.
                regionName is a human-readable place name from the user, never an administrative ID.
                Use only categories implied by the user. Use equal weights if no priority is implied.
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

    private static List<GroqMessage> BuildExplanationMessages(
        ExperienceRecommendationPreferences preferences,
        IReadOnlyList<ExperienceRecommendationItemResponse> rankedItems,
        bool compact)
    {
        var payload = JsonSerializer.Serialize(new
        {
            preferences = new
            {
                preferences.Categories,
                preferences.CrowdPreference,
                preferences.ForItinerary,
                preferences.PreferredLanguage
            },
            rankedExperiences = rankedItems.Select(x => new
            {
                experienceId = x.ExperienceId,
                x.Name,
                x.Category,
                x.RegionDisplayName,
                x.DistanceKm,
                x.Rating,
                x.Reviews,
                x.FinalScore,
                x.Scores,
                x.OpenHoursDataAvailable,
                x.PopularTimesDataAvailable,
                x.AvailabilityDataAvailable,
                x.EstimatedDurationMinutes,
                amenities = compact ? [] : x.Amenities.Take(4)
            })
        }, JsonOptions);

        return
        [
            new GroqMessage
            {
                Role = "system",
                Content = """
                Explain already-ranked tourism experience recommendations.
                Backend scoring already decided ranking. Do not reorder or invent facts.
                Return strict JSON only:
                {"items":[{"experienceId":1,"shortExplanation":"","reasons":["",""],"bestFor":["Historical"]}]}
                Keep each explanation useful but short: 1-2 sentences and 2-3 practical reasons.
                Mention distance, category fit, quality, timing/crowd/availability only when provided.
                Use Arabic if preferredLanguage is ar; otherwise English.
                """
            },
            new GroqMessage { Role = "user", Content = payload }
        ];
    }

    private static ExperienceRecommendationPreferences ParseClassification(string content)
    {
        using var document = JsonDocument.Parse(ExtractJsonObject(content));
        var root = document.RootElement;
        var preferences = new ExperienceRecommendationPreferences
        {
            PreferredLanguage = ReadString(root, "preferredLanguage") ?? "en",
            ClassificationConfidence = ReadNullableDouble(root, "classificationConfidence"),
            Notes = ReadString(root, "notes"),
            ForItinerary = ReadNullableBool(root, "forItinerary") ?? false,
            VisitAtLocal = ReadNullableDateTime(root, "visitAtLocal")
            ,RegionName = ReadString(root, "regionName")
        };

        if (Enum.TryParse<ExperienceCrowdPreference>(ReadString(root, "crowdPreference"), true, out var crowd))
        {
            preferences.CrowdPreference = crowd;
        }

        if (root.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in categories.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !Enum.TryParse<ExperienceCategory>(ReadString(item, "category"), true, out var category))
                {
                    continue;
                }

                preferences.Categories.Add(new WeightedExperienceRecommendationCategory
                {
                    Category = category,
                    Weight = ReadNullableDouble(item, "weight") ?? 1
                });
            }
        }

        return preferences;
    }

    private static IReadOnlyDictionary<int, ExperienceRecommendationExplanationResponse> ParseExplanations(
        string content,
        IReadOnlyList<ExperienceRecommendationItemResponse> rankedItems)
    {
        var knownIds = rankedItems.Select(x => x.ExperienceId).ToHashSet();
        using var document = JsonDocument.Parse(ExtractJsonObject(content));
        var root = document.RootElement;
        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Groq explanation response did not contain an items array.");
        }

        var result = new Dictionary<int, ExperienceRecommendationExplanationResponse>();
        foreach (var item in items.EnumerateArray())
        {
            var id = ReadNullableInt(item, "experienceId");
            if (id is null || !knownIds.Contains(id.Value))
            {
                continue;
            }

            var explanation = new ExperienceRecommendationExplanationResponse
            {
                ShortExplanation = ReadString(item, "shortExplanation") ?? string.Empty,
                Reasons = ReadStringArray(item, "reasons").Take(3).ToList(),
                BestFor = ReadStringArray(item, "bestFor").Take(4).ToList()
            };

            if (!string.IsNullOrWhiteSpace(explanation.ShortExplanation))
            {
                result[id.Value] = explanation;
            }
        }

        return result;
    }

    private string ResolveApiKey() =>
        FirstConfigured(_options.GroqApiKey, Environment.GetEnvironmentVariable("GROQ_API_KEY"));

    private string ResolveModel() =>
        FirstConfigured(_options.GroqModel, Environment.GetEnvironmentVariable("GROQ_MODEL"), "llama-3.1-8b-instant");

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

    private static DateTime? ReadNullableDateTime(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String &&
        DateTime.TryParse(property.GetString(), out var value)
            ? value
            : null;

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
    }

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

    private sealed class GroqHttpRequestException : HttpRequestException
    {
        public GroqHttpRequestException(HttpStatusCode statusCode, string responseBody)
            : base($"Groq request failed with {(int)statusCode} ({statusCode}). Response: {responseBody}", null, statusCode)
        {
            ResponseBody = responseBody;
        }

        public string ResponseBody { get; }
    }
}
