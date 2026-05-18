using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Glinter.Modules.LocationCatalog.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Services;

public sealed class GroqSafetyAiAnalyzer : ISafetyAiAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;

    public GroqSafetyAiAnalyzer(
        HttpClient httpClient,
        IOptions<GroqOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<SafetyAiAnalysisResult> AnalyzeAsync(
        string districtName,
        string text,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new GroqChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = _options.Temperature,
            MaxTokens = _options.MaxTokens,
            Messages =
            [
                new GroqMessage
                {
                    Role = "system",
                    Content = BuildSystemPrompt()
                },
                new GroqMessage
                {
                    Role = "user",
                    Content = BuildUserPrompt(districtName, text)
                }
            ]
        };

        request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Groq safety analyzer failed. Status: {(int)response.StatusCode}. Response: {responseBody}");
        }

        var completion = JsonSerializer.Deserialize<GroqChatCompletionResponse>(
            responseBody,
            JsonOptions);

        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Groq safety analyzer returned an empty response.");
        }

        var json = ExtractJsonObject(content);

        var result = JsonSerializer.Deserialize<SafetyAiAnalysisResult>(
            json,
            JsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException("Failed to parse safety analysis JSON.");
        }

        return NormalizeResult(result);
    }

    private string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return _options.ApiKey;
        }

        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GROQ_API_KEY was not found. Add it as an environment variable before running the app.");
        }

        return apiKey;
    }

    private static string BuildSystemPrompt()
    {
        return """
        You are a safety analysis assistant for an Egypt-focused travel planning platform.

        Your task is to analyze text related to a district or neighborhood and classify whether it contains a safety-related signal for travelers.

        Return JSON only. Do not include markdown, explanations, or extra text.

        Use this exact JSON shape:
        {
          "riskCategory": "none | theft | harassment | scam | protest | traffic | violence | accident | other",
          "severity": "none | low | medium | high | critical",
          "confidence": 0.0,
          "sentimentScore": 0.0,
          "summary": "short explanation",
          "isSafetyRelevant": true
        }

        Rules:
        - confidence must be between 0 and 1.
        - sentimentScore must be between -1 and 1.
        - If the text is not safety-related, use:
          riskCategory = "none",
          severity = "none",
          isSafetyRelevant = false.
        - Do not exaggerate risk.
        - Use high or critical only for serious, repeated, violent, or urgent incidents.
        - Focus on traveler safety.
        """;
    }

    private static string BuildUserPrompt(string districtName, string text)
    {
        return $"""
        District: {districtName}

        Text to analyze:
        {text}
        """;
    }

    private static string ExtractJsonObject(string content)
    {
        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');

        if (firstBrace < 0 || lastBrace < 0 || lastBrace <= firstBrace)
        {
            throw new InvalidOperationException($"LLM response did not contain valid JSON. Response: {content}");
        }

        return content[firstBrace..(lastBrace + 1)];
    }

    private static SafetyAiAnalysisResult NormalizeResult(SafetyAiAnalysisResult result)
    {
        var riskCategory = NormalizeRiskCategory(result.RiskCategory);
        var severity = NormalizeSeverity(result.Severity);

        var confidence = Math.Clamp(result.Confidence, 0, 1);
        var sentimentScore = Math.Clamp(result.SentimentScore, -1, 1);

        var isSafetyRelevant = result.IsSafetyRelevant;

        if (riskCategory == "none" && severity == "none")
        {
            isSafetyRelevant = false;
        }

        return new SafetyAiAnalysisResult(
            riskCategory,
            severity,
            confidence,
            sentimentScore,
            string.IsNullOrWhiteSpace(result.Summary)
                ? "No summary was provided."
                : result.Summary.Trim(),
            isSafetyRelevant
        );
    }

    private static string NormalizeRiskCategory(string value)
    {
        var normalized = value.Trim().ToLower();

        return normalized switch
        {
            "none" => "none",
            "theft" => "theft",
            "harassment" => "harassment",
            "scam" => "scam",
            "protest" => "protest",
            "traffic" => "traffic",
            "violence" => "violence",
            "accident" => "accident",
            "other" => "other",
            _ => "other"
        };
    }

    private static string NormalizeSeverity(string value)
    {
        var normalized = value.Trim().ToLower();

        return normalized switch
        {
            "none" => "none",
            "low" => "low",
            "medium" => "medium",
            "high" => "high",
            "critical" => "critical",
            _ => "none"
        };
    }

    private sealed class GroqChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<GroqMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    private sealed class GroqMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class GroqChatCompletionResponse
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