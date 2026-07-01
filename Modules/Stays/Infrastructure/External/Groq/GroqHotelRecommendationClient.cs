using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Dtos;
using Glinter.Modules.Stays.Application.Options;
using Glinter.Modules.Stays.Application.Services;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Stays.Infrastructure.External.Groq;

public class GroqHotelRecommendationClient(
    HttpClient httpClient,
    IOptions<HotelRecommendationOptions> options,
    ILogger<GroqHotelRecommendationClient> logger) : IHotelRecommendationGroqClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HotelRecommendationOptions _options = options.Value;

    public async Task<HotelRecommendationPreferences?> ClassifyAsync(
        string text,
        string? preferredLanguage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ResolveApiKey()))
        {
            logger.LogWarning("Groq API key is missing. Using local hotel recommendation text classification fallback.");
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
            logger.LogWarning(ex, "Groq hotel recommendation classification failed.");
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<int, HotelRecommendationExplanationResponse>?> GenerateExplanationsAsync(
        HotelRecommendationRequest request,
        IReadOnlyList<HotelRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ResolveApiKey()))
        {
            logger.LogWarning("Groq API key is missing. Using local hotel recommendation explanation fallback.");
            return null;
        }

        try
        {
            var itemsToExplain = rankedItems
                .Take(Math.Clamp(_options.GroqMaxExplanationItems, 1, 20))
                .ToList();

            var response = await SendChatAsync(
                BuildExplanationMessages(request, itemsToExplain, compact: false),
                temperature: 0.2m,
                maxTokens: Math.Clamp(_options.GroqExplanationMaxTokens, 300, 1200),
                useJsonResponseFormat: true,
                cancellationToken);

            var content = response?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? null : ParseExplanations(content, rankedItems);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            logger.LogWarning(
                ex,
                "Groq hotel recommendation explanation payload was too large. Retrying with compact payload.");

            return await RetryGenerateCompactExplanationsAsync(request, rankedItems, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Groq hotel recommendation explanations failed.");
            return null;
        }
    }

    private async Task<IReadOnlyDictionary<int, HotelRecommendationExplanationResponse>?> RetryGenerateCompactExplanationsAsync(
        HotelRecommendationRequest request,
        IReadOnlyList<HotelRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken)
    {
        try
        {
            var retryItems = rankedItems
                .Take(Math.Clamp(_options.GroqRetryMaxExplanationItems, 1, 8))
                .ToList();

            var response = await SendChatAsync(
                BuildExplanationMessages(request, retryItems, compact: true),
                temperature: 0.2m,
                maxTokens: Math.Clamp(_options.GroqExplanationMaxTokens, 300, 900),
                useJsonResponseFormat: true,
                cancellationToken);

            var content = response?.Choices.FirstOrDefault()?.Message.Content;
            return string.IsNullOrWhiteSpace(content) ? null : ParseExplanations(content, rankedItems);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Compact Groq hotel recommendation explanations failed.");
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
            return await SendChatOnceAsync(
                messages,
                temperature,
                maxTokens,
                useJsonResponseFormat,
                cancellationToken);
        }
        catch (GroqHttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.BadRequest &&
                  useJsonResponseFormat &&
                  _options.GroqRetryWithoutResponseFormatOnBadRequest)
        {
            logger.LogWarning(
                "Groq rejected JSON response_format with 400. Retrying without response_format. Response: {GroqResponseBody}",
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
            ResponseFormat = useJsonResponseFormat
                ? new GroqResponseFormat { Type = "json_object" }
                : null,
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

    private static List<GroqMessage> BuildClassificationMessages(
        string text,
        string? preferredLanguage)
    {
        return
        [
            new GroqMessage
            {
                Role = "system",
                Content = """
                You convert hotel recommendation text into structured preferences.
                Return only valid JSON with this exact shape:
                {
                  "budgetLevel": 3,
                  "experienceCategories": [{"category":"Historical","weight":1}],
                  "requestedAmenities": ["WiFi"],
                  "preferredLanguage": "ar",
                  "classificationConfidence": 0.8,
                  "notes": ""
                }

                Supported budget levels:
                1 Budget, 2 Economy, 3 MidRange, 4 Upscale, 5 Luxury.

                Supported categories only:
                Historical, Nature, Shopping, Nightlife, Dining.

                Supported amenity tags include:
                WiFi, Gym, Pool, Spa, Restaurant, Bar, Parking.

                Use null for budgetLevel if unclear.
                Use equal category weights when the user does not imply priority.
                Return Arabic preferredLanguage for Arabic user text, otherwise English.
                Do not add categories or amenities that are not implied by the text.
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

    private List<GroqMessage> BuildExplanationMessages(
        HotelRecommendationRequest request,
        IReadOnlyList<HotelRecommendationItemResponse> rankedItems,
        bool compact)
    {
        var descriptionCharacters = compact
            ? 0
            : Math.Clamp(_options.GroqMaxDescriptionCharacters, 0, 400);
        var locationCharacters = Math.Clamp(
            compact ? _options.GroqMaxLocationSummaryCharacters / 2 : _options.GroqMaxLocationSummaryCharacters,
            0,
            300);
        var amenitiesLimit = Math.Clamp(compact ? _options.GroqMaxAmenitiesPerHotel / 2 : _options.GroqMaxAmenitiesPerHotel, 0, 12);
        var nearbyLimit = Math.Clamp(compact ? 2 : _options.GroqMaxNearbyExperiencesPerHotel, 1, 5);

        var explanationInput = rankedItems.Select(item => new
        {
            hotelId = item.HotelId,
            name = item.Name,
            price = item.Price,
            description = descriptionCharacters <= 0 ? null : Truncate(item.Description, descriptionCharacters),
            locationSummary = locationCharacters <= 0 ? null : Truncate(item.LocationSummaryDescription, locationCharacters),
            budgetLevel = item.BudgetLevel,
            budgetLabel = item.BudgetLabel,
            region = BuildCompactRegion(item.Region, request.PreferredLanguage),
            rating = item.Rating,
            reviews = item.Reviews,
            finalScore = item.FinalScore,
            scores = item.Scores,
            amenities = item.Amenities.Take(amenitiesLimit),
            matchedAmenities = item.MatchedAmenities,
            nearbyExperiences = item.NearbyExperiences
                .OrderBy(x => x.DistanceKm)
                .Take(nearbyLimit)
                .Select(x => new
                {
                    x.Name,
                    x.Category,
                    x.DistanceKm
                })
        });

        var payload = JsonSerializer.Serialize(new
        {
            userPreferences = new
            {
                request.BudgetLevel,
                request.ExperienceCategories,
                request.RequestedAmenities,
                request.PreferredLanguage
            },
            rankedHotels = explanationInput
        }, JsonOptions);

        return
        [
            new GroqMessage
            {
                Role = "system",
                Content = """
                You explain already-ranked hotel recommendations.
                The backend algorithm already decided ranking. Do not reorder hotels and do not change scores.
                Do not invent hotel facts. Use only the provided data.
                Return only valid JSON with this exact shape:
                {
                  "items": [
                    {
                      "hotelId": 1,
                      "shortExplanation": "",
                      "reasons": ["", "", ""],
                      "bestFor": ["Luxury", "Shopping"]
                    }
                  ]
                }

                Make the explanation useful and specific, but keep it moderately short.
                shortExplanation should be 1-2 natural sentences.
                reasons should contain 2-3 practical, non-repetitive reasons when the data supports them.
                Explain the tradeoffs clearly: budget fit, proximity to selected experience categories, quality confidence from rating/reviews, and matched amenities.
                Use hotel-specific context such as region names, location summary, description, available amenities, and nearby experience names to make each hotel's reasons distinct.
                Prefer region names over administrative IDs when explaining location.
                Use the score breakdown to explain why this hotel ranked well, but do not expose formula weights.
                If a factor is weak or missing, mention it gently only when it helps the user make a better choice.
                Mention budget fit only when budget data is provided.
                Mention proximity to selected categories when nearby experiences are provided.
                Mention rating/review confidence when provided.
                Mention amenities only when matched amenities are provided.
                Use Arabic if preferredLanguage is ar; otherwise use English.
                """
            },
            new GroqMessage
            {
                Role = "user",
                Content = payload
            }
        ];
    }

    private static CompactRegion? BuildCompactRegion(
        HotelRecommendationRegionResponse? region,
        string? preferredLanguage)
    {
        if (region is null)
        {
            return null;
        }

        var useArabic = preferredLanguage?.StartsWith("ar", StringComparison.OrdinalIgnoreCase) == true;
        var neighbourhood = PickLocalized(region.NeighbourhoodNameEn, region.NeighbourhoodNameAr, useArabic);
        var district = PickLocalized(region.DistrictNameEn, region.DistrictNameAr, useArabic);
        var governorate = PickLocalized(region.GovernorateNameEn, region.GovernorateNameAr, useArabic);
        var country = PickLocalized(region.CountryNameEn, region.CountryNameAr, useArabic);
        var displayName = string.Join(", ", new[] { neighbourhood, district, governorate, country }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase));

        return new CompactRegion
        {
            Area = string.IsNullOrWhiteSpace(displayName) ? region.DisplayName : displayName,
            Governorate = governorate,
            Neighbourhood = neighbourhood
        };
    }

    private static string? PickLocalized(string? nameEn, string? nameAr, bool useArabic)
    {
        return useArabic
            ? FirstConfigured(nameAr, nameEn)
            : FirstConfigured(nameEn, nameAr);
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

    private static HotelRecommendationPreferences ParseClassification(string content)
    {
        using var document = JsonDocument.Parse(ExtractJsonObject(content));
        var root = document.RootElement;

        var preferences = new HotelRecommendationPreferences
        {
            BudgetLevel = ReadNullableInt(root, "budgetLevel"),
            PreferredLanguage = ReadString(root, "preferredLanguage") ?? "en",
            ClassificationConfidence = ReadNullableDouble(root, "classificationConfidence"),
            Notes = ReadString(root, "notes")
        };

        if (root.TryGetProperty("experienceCategories", out var categories) &&
            categories.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in categories.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    preferences.ExperienceCategories.Add(new WeightedExperienceCategoryPreference
                    {
                        Category = item.GetString() ?? string.Empty,
                        Weight = 1
                    });
                    continue;
                }

                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                preferences.ExperienceCategories.Add(new WeightedExperienceCategoryPreference
                {
                    Category = ReadString(item, "category") ?? string.Empty,
                    Weight = ReadNullableDouble(item, "weight") ?? 1
                });
            }
        }

        if (root.TryGetProperty("categoryWeights", out var weights) &&
            weights.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in weights.EnumerateObject())
            {
                preferences.ExperienceCategories.Add(new WeightedExperienceCategoryPreference
                {
                    Category = property.Name,
                    Weight = property.Value.TryGetDouble(out var value) ? value : 1
                });
            }
        }

        if (root.TryGetProperty("requestedAmenities", out var amenities) &&
            amenities.ValueKind == JsonValueKind.Array)
        {
            preferences.RequestedAmenities = amenities
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => HotelRecommendationService.NormalizeAmenityTag(x.GetString()))
                .Where(x => x is not null)
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return preferences;
    }

    private static IReadOnlyDictionary<int, HotelRecommendationExplanationResponse> ParseExplanations(
        string content,
        IReadOnlyList<HotelRecommendationItemResponse> rankedItems)
    {
        var knownHotelIds = rankedItems.Select(x => x.HotelId).ToHashSet();
        using var document = JsonDocument.Parse(ExtractJsonObject(content));
        var root = document.RootElement;
        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Groq explanation response did not contain an items array.");
        }

        var result = new Dictionary<int, HotelRecommendationExplanationResponse>();
        foreach (var item in items.EnumerateArray())
        {
            var hotelId = ReadNullableInt(item, "hotelId");
            if (hotelId is null || !knownHotelIds.Contains(hotelId.Value))
            {
                continue;
            }

            var explanation = new HotelRecommendationExplanationResponse
            {
                ShortExplanation = ReadString(item, "shortExplanation") ?? string.Empty,
                Reasons = ReadStringArray(item, "reasons").Take(4).ToList(),
                BestFor = ReadStringArray(item, "bestFor").Take(5).ToList()
            };

            if (!string.IsNullOrWhiteSpace(explanation.ShortExplanation))
            {
                result[hotelId.Value] = explanation;
            }
        }

        return result;
    }

    private string ResolveApiKey()
    {
        return FirstConfigured(
            _options.GroqApiKey,
            Environment.GetEnvironmentVariable("GROQ_API_KEY"));
    }

    private string ResolveModel()
    {
        return FirstConfigured(
            _options.GroqModel,
            Environment.GetEnvironmentVariable("GROQ_MODEL"),
            "llama-3.1-8b-instant");
    }

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

    private static string FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

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

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? ReadNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? ReadNullableDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static List<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
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

    private sealed class CompactRegion
    {
        [JsonPropertyName("area")]
        public string? Area { get; set; }

        [JsonPropertyName("governorate")]
        public string? Governorate { get; set; }

        [JsonPropertyName("neighbourhood")]
        public string? Neighbourhood { get; set; }
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
            : base(
                $"Groq request failed with {(int)statusCode} ({statusCode}). Response: {responseBody}",
                inner: null,
                statusCode)
        {
            ResponseBody = responseBody;
        }

        public string ResponseBody { get; }
    }
}
