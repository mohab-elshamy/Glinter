using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.SafetyIndex.Application.Abstractions;
using Glinter.Modules.SafetyIndex.Application.Dtos;
using Glinter.Modules.SafetyIndex.Application.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.SafetyIndex.Infrastructure.External.Groq;

public class GroqSafetyScoringClient(
    HttpClient httpClient,
    GroqRequestRateLimiter rateLimiter,
    IOptions<SafetyIndexOptions> options,
    ILogger<GroqSafetyScoringClient> logger) : ISafetyScoringClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SafetyIndexOptions options = options.Value;

    public async Task<SafetyScoreEstimateDto?> EstimateAsync(
        string areaNameAr,
        IReadOnlyCollection<string> titles,
        SafetyScorePeriod period,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.GroqApiKey))
        {
            logger.LogWarning("Groq API key is missing. Skipping safety scoring for {AreaNameAr}", areaNameAr);
            return null;
        }

        var selectedTitles = SelectTitles(titles);

        if (selectedTitles.Length == 0)
        {
            return new SafetyScoreEstimateDto
            {
                Score = 50,
                GeneralSafetyDescription = period == SafetyScorePeriod.Historical
                    ? "لا توجد أخبار تاريخية كافية لفهم الجو العام للمنطقة بثقة."
                    : "لا توجد أخبار حديثة كافية لتقدير مستوى السلامة الحالي بثقة.",
                TrendingEventDescription = "لا يظهر نمط متكرر واضح في الأخبار المتاحة.",
            };
        }

        var groqResponse = await SendScoringRequestAsync(areaNameAr, selectedTitles, period, ct);
        var content = groqResponse?.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("Groq returned an empty safety scoring response for {AreaNameAr}", areaNameAr);
            return null;
        }

        var estimate = ParseEstimate(content);
        estimate.Score = Math.Clamp(estimate.Score, 0, 100);
        return estimate;
    }

    private async Task<GroqChatResponse?> SendScoringRequestAsync(
        string areaNameAr,
        IReadOnlyList<string> selectedTitles,
        SafetyScorePeriod period,
        CancellationToken ct)
    {
        var titlesToSend = selectedTitles.ToArray();
        const int maxPayloadAttempts = 3;

        for (var attempt = 1; attempt <= maxPayloadAttempts; attempt++)
        {
            using var request = BuildRequest(areaNameAr, titlesToSend, period);

            logger.LogInformation(
                "Sending {TitleCount} news titles to Groq model {Model} for {AreaNameAr}",
                titlesToSend.Length,
                options.GroqModel,
                areaNameAr);

            await rateLimiter.WaitAsync(ct);

            using var response = await httpClient.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge && titlesToSend.Length > 1)
            {
                var nextCount = Math.Max(1, titlesToSend.Length / 2);
                logger.LogWarning(
                    "Groq rejected the safety scoring payload as too large for {AreaNameAr}. Retrying with {TitleCount} titles instead of {PreviousTitleCount}",
                    areaNameAr,
                    nextCount,
                    titlesToSend.Length);

                titlesToSend = titlesToSend.Take(nextCount).ToArray();
                continue;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = GetRetryAfter(response);
                rateLimiter.Defer(retryAfter);
                logger.LogWarning(
                    "Groq quota was reached while scoring {AreaNameAr}. Skipping this score and pausing requests for {RetryAfterSeconds} seconds",
                    areaNameAr,
                    Math.Ceiling(retryAfter.TotalSeconds));
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GroqChatResponse>(JsonOptions, ct);
        }

        throw new HttpRequestException(
            $"Groq safety scoring payload is too large after {maxPayloadAttempts} attempts for {areaNameAr}.");
    }

    private static TimeSpan GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromMinutes(1);
    }

    private HttpRequestMessage BuildRequest(
        string areaNameAr,
        IReadOnlyList<string> selectedTitles,
        SafetyScorePeriod period)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, options.GroqChatCompletionsUrl);
        request.Headers.Authorization = new("Bearer", options.GroqApiKey);

        var payload = new GroqChatRequest
        {
            Model = options.GroqModel,
            Temperature = 0.1m,
            ResponseFormat = new GroqResponseFormat { Type = "json_object" },
            Messages =
            [
                new GroqMessage
                {
                    Role = "system",
                    Content = BuildSystemPrompt(period),
                },
                new GroqMessage
                {
                    Role = "user",
                    Content = $"""
                    المنطقة: {areaNameAr}
                    نوع التقييم: {(period == SafetyScorePeriod.Historical ? "تاريخي عام" : "أسبوعي حديث")}

                    عناوين الأخبار:
                    {string.Join('\n', selectedTitles.Select((title, index) => $"{index + 1}. {title}"))}
                    """,
                },
            ],
        };

        request.Content = JsonContent.Create(payload, options: JsonOptions);
        return request;
    }

    private string[] SelectTitles(IReadOnlyCollection<string> titles)
    {
        var maxTitles = Math.Max(1, options.MaxTitlesPerPrompt);
        var maxTitleCharacters = Math.Max(20, options.GroqMaxTitleCharacters);
        var maxPromptTitleCharacters = Math.Max(maxTitleCharacters, options.GroqMaxPromptTitleCharacters);
        var selectedTitles = new List<string>();
        var totalCharacters = 0;

        foreach (var title in titles.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal))
        {
            if (selectedTitles.Count >= maxTitles)
            {
                break;
            }

            var normalizedTitle = NormalizeTitle(title, maxTitleCharacters);
            var nextTotal = totalCharacters + normalizedTitle.Length;
            if (selectedTitles.Count > 0 && nextTotal > maxPromptTitleCharacters)
            {
                break;
            }

            selectedTitles.Add(normalizedTitle);
            totalCharacters = nextTotal;
        }

        return selectedTitles.ToArray();
    }

    private static string NormalizeTitle(string title, int maxTitleCharacters)
    {
        var normalized = string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length <= maxTitleCharacters)
        {
            return normalized;
        }

        return $"{normalized[..maxTitleCharacters].Trim()}...";
    }

    private static string BuildSystemPrompt(SafetyScorePeriod period)
    {
        var periodInstructions = period == SafetyScorePeriod.Historical
            ? """
              This is a historical/general score based on all collected older and recent news.
              generalSafetyDescription must explain the area's general long-term atmosphere for a tourist, not what they should do at the current moment.
              Do not give current-time travel instructions, warnings, or advice such as "اذهب الآن", "تجنب الآن", "انتبه حاليا", or "يمكنك زيارة المنطقة الآن".
              Prefer neutral context phrasing like "ستفهم أن الطابع العام للمنطقة..." or "تاريخيا، الأخبار حول المنطقة تميل إلى...".
              trendingEventDescription should summarize the repeated historical pattern only, without implying it is happening now.
              """
            : """
              This is a weekly/current score based only on recent news.
              generalSafetyDescription should briefly tell the tourist whether they can visit comfortably now and mention simple current precautions if useful.
              trendingEventDescription should briefly tell the tourist what repeated or noticeable recent news pattern they should be aware of and why it matters now.
              Address the tourist directly using second-person phrasing.
              Avoid detached phrasing like "المنطقة تبدو آمنة" or "لا توجد مؤشرات"; prefer direct phrasing like "يمكنك زيارة المنطقة..." and "انتبه إلى...".
              """;

        return $$"""
        You estimate public safety for tourists from Arabic Google News titles.
        Return only valid JSON with this exact shape:
        {"score":0,"generalSafetyDescription":"","trendingEventDescription":""}
        score must be an integer from 0 to 100. 0 means very unsafe and 100 means very safe.
        Write both descriptions in Arabic as short, practical travel insights.
        Keep each description to 1-2 concise sentences.
        Be cautious and do not invent events not supported by titles.

        {{periodInstructions}}
        """;
    }

    private static SafetyScoreEstimateDto ParseEstimate(string content)
    {
        var json = ExtractJsonObject(content);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new SafetyScoreEstimateDto
        {
            Score = root.TryGetProperty("score", out var score) && score.TryGetInt32(out var value) ? value : 0,
            GeneralSafetyDescription = root.TryGetProperty("generalSafetyDescription", out var general)
                ? general.GetString() ?? string.Empty
                : string.Empty,
            TrendingEventDescription = root.TryGetProperty("trendingEventDescription", out var trending)
                ? trending.GetString() ?? string.Empty
                : string.Empty,
        };
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

    private sealed class GroqChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<GroqMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }

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
}
