using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class ItineraryCommunicationCriticalTests : ApiTestBase
{
    public ItineraryCommunicationCriticalTests(GlinterApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Saved_itinerary_is_not_visible_to_another_user()
    {
        // Arrange
        var owner = await CreateUserAsync("Traveler", "itinerary-owner");
        var otherTraveler = await CreateUserAsync("Traveler", "itinerary-other");
        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var save = await SendAsync(
            HttpMethod.Post,
            "/api/itineraries",
            owner.Token,
            new
            {
                title = "Private itinerary",
                destination = "Cairo",
                startDate = tripDate,
                endDate = tripDate,
                preferredLanguage = "en",
                items = Array.Empty<object>()
            });
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        using var saveJson = await ReadJsonAsync(save);
        var itineraryId = saveJson.RootElement.GetProperty("id").GetGuid();

        // Act
        var forbiddenRead = await SendAsync(
            HttpMethod.Get,
            $"/api/itineraries/{itineraryId}",
            otherTraveler.Token);
        var otherList = await SendAsync(
            HttpMethod.Get,
            "/api/itineraries",
            otherTraveler.Token);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, forbiddenRead.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherList.StatusCode);
        using var listJson = await ReadJsonAsync(otherList);
        Assert.DoesNotContain(
            listJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == itineraryId);
        Assert.Equal(
            owner.UserId,
            await Factory.ScalarAsync<Guid>(
                $"""
                 SELECT "UserId"
                 FROM itineraries.saved_itineraries
                 WHERE "Id" = '{itineraryId}'
                 """));
    }

    [Fact]
    public async Task Natural_language_itinerary_uses_local_fallback_when_ai_is_unavailable()
    {
        // Arrange
        var tripDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        // Act
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/itineraries/plan/natural-language",
            body: new
            {
                text = "Plan a relaxed cultural day",
                start = new { latitude = 30.0444, longitude = 31.2357 },
                date = tripDate,
                startDate = tripDate,
                endDate = tripDate,
                maxStops = 2,
                preferredLanguage = "en"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.False(
            json.RootElement.GetProperty("classification").GetProperty("isAiGenerated").GetBoolean());
        Assert.Equal(
            "Plan a relaxed cultural day",
            json.RootElement.GetProperty("inputText").GetString());
        Assert.Equal(
            1,
            json.RootElement.GetProperty("itinerary").GetProperty("days").GetArrayLength());
    }

    [Fact]
    public async Task Non_participant_cannot_send_a_message_to_a_direct_thread()
    {
        // Arrange
        var participantA = await CreateUserAsync("Traveler", "send-owner-a");
        var participantB = await CreateUserAsync("Traveler", "send-owner-b");
        var outsider = await CreateUserAsync("Traveler", "send-outsider");
        var create = await SendAsync(
            HttpMethod.Post,
            "/api/chat/threads/direct",
            participantA.Token,
            new { otherUserId = participantB.UserId });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        using var createJson = await ReadJsonAsync(create);
        var threadId = createJson.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/chat/threads/{threadId}/messages",
            outsider.Token,
            new { body = "This must not be persisted." });

        // Assert
        await AssertProblemAsync(response, 403, "forbidden");
        Assert.Equal(
            0,
            await Factory.ScalarAsync<long>(
                $"""SELECT count(*) FROM chat_messages WHERE "ThreadId" = '{threadId}'"""));
    }
}
