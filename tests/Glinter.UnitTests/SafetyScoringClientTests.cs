using System.Net;
using System.Text;
using Glinter.Modules.SafetyIndex.Application.Dtos;
using Glinter.Modules.SafetyIndex.Application.Options;
using Glinter.Modules.SafetyIndex.Infrastructure.External.Groq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Glinter.UnitTests;

public sealed class SafetyScoringClientTests
{
    [Fact]
    public async Task External_safety_scores_are_clamped_to_the_supported_range()
    {
        // Arrange, Act, Assert
        foreach (var (externalScore, expectedScore) in new[] { (140, 100), (-20, 0) })
        {
            var client = CreateClient(
                new StubHttpMessageHandler(_ => JsonResponse(externalScore)));

            var result = await client.EstimateAsync(
                "القاهرة",
                ["خبر أمني"],
                SafetyScorePeriod.Weekly);

            Assert.NotNull(result);
            Assert.Equal(expectedScore, result.Score);
        }
    }

    [Fact]
    public async Task Empty_news_uses_neutral_fallback_without_calling_external_scoring()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(
            _ => throw new InvalidOperationException("External HTTP must not be called."));
        var client = CreateClient(handler);

        // Act
        var result = await client.EstimateAsync(
            "القاهرة",
            Array.Empty<string>(),
            SafetyScorePeriod.Historical);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Score);
        Assert.Equal(0, handler.CallCount);
        Assert.False(string.IsNullOrWhiteSpace(result.GeneralSafetyDescription));
    }

    private static GroqSafetyScoringClient CreateClient(
        HttpMessageHandler handler)
    {
        var options = new SafetyIndexOptions
        {
            GroqApiKey = "unit-test-key",
            GroqChatCompletionsUrl = "https://unit.test/chat",
            GroqRequestsPerMinute = 0
        };
        var monitor = new FixedOptionsMonitor<SafetyIndexOptions>(options);
        var limiter = new GroqRequestRateLimiter(
            monitor,
            NullLogger<GroqRequestRateLimiter>.Instance);
        return new GroqSafetyScoringClient(
            new HttpClient(handler),
            limiter,
            Options.Create(options),
            NullLogger<GroqSafetyScoringClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(int score) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            $$"""
              {
                "choices": [
                  {
                    "message": {
                      "role": "assistant",
                      "content": "{\"score\":{{score}},\"generalSafetyDescription\":\"General\",\"trendingEventDescription\":\"Trend\"}"
                    }
                  }
                ]
              }
              """,
            Encoding.UTF8,
            "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
