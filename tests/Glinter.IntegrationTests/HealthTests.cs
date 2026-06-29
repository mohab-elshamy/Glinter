using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class HealthTests : ApiTestBase
{
    public HealthTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Liveness_and_readiness_are_public_and_healthy()
    {
        var live = await Client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("Healthy", await live.Content.ReadAsStringAsync());

        var ready = await Client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal("Healthy", await ready.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Correlation_ids_are_propagated_and_used_by_problem_details()
    {
        const string correlationId = "frontend-request-123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", correlationId);
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Equal(
            correlationId,
            response.Headers.GetValues("X-Correlation-ID").Single());

        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        invalidRequest.Headers.Add("X-Correlation-ID", "invalid correlation id!");
        var problemResponse = await Client.SendAsync(invalidRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, problemResponse.StatusCode);
        var generatedId = problemResponse.Headers
            .GetValues("X-Correlation-ID")
            .Single();
        Assert.NotEqual("invalid correlation id!", generatedId);
        using var problemJson = await ReadJsonAsync(problemResponse);
        Assert.Equal(
            "authentication_required",
            problemJson.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(
            generatedId,
            problemJson.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_a_schema_migration_is_pending()
    {
        const string migrationId =
            "20260628000712_AddLocalBuddyVerificationHistory";
        var removed = await Factory.ExecuteAsync(
            $"""
             DELETE FROM "__EFMigrationsHistory"
             WHERE "MigrationId" = '{migrationId}'
             """);
        Assert.Equal(1, removed);

        try
        {
            var unhealthy = await Client.GetAsync("/health/ready");
            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                unhealthy.StatusCode);
            Assert.Equal("Unhealthy", await unhealthy.Content.ReadAsStringAsync());
        }
        finally
        {
            await Factory.ExecuteAsync(
                $"""
                 INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                 VALUES ('{migrationId}', '10.0.6')
                 ON CONFLICT ("MigrationId") DO NOTHING
                 """);
        }

        var healthy = await Client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
    }
}
