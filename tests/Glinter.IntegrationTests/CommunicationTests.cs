using System.Reflection;
using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Shared.Application.Exceptions;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class CommunicationTests : ApiTestBase
{
    public CommunicationTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Threads_are_unique_private_and_rate_limited()
    {
        var userA = await CreateUserAsync("Traveler", "chat-a");
        var userB = await CreateUserAsync("Traveler", "chat-b");
        var userC = await CreateUserAsync("Traveler", "chat-c");

        var firstThread = await SendAsync(
            HttpMethod.Post,
            "/api/chat/threads/direct",
            userA.Token,
            new { otherUserId = userB.UserId });
        firstThread.EnsureSuccessStatusCode();
        using var firstThreadJson = await ReadJsonAsync(firstThread);
        var threadId = firstThreadJson.RootElement.GetProperty("id").GetGuid();

        var duplicateThread = await SendAsync(
            HttpMethod.Post,
            "/api/chat/threads/direct",
            userA.Token,
            new { otherUserId = userB.UserId });
        duplicateThread.EnsureSuccessStatusCode();
        using var duplicateThreadJson = await ReadJsonAsync(duplicateThread);
        Assert.Equal(
            threadId,
            duplicateThreadJson.RootElement.GetProperty("id").GetGuid());

        var message = await SendAsync(
            HttpMethod.Post,
            $"/api/chat/threads/{threadId}/messages",
            userA.Token,
            new { body = "Integration message" });
        message.EnsureSuccessStatusCode();

        var participantMessages = await SendAsync(
            HttpMethod.Get,
            $"/api/chat/threads/{threadId}/messages",
            userB.Token);
        participantMessages.EnsureSuccessStatusCode();

        var nonParticipantMessages = await SendAsync(
            HttpMethod.Get,
            $"/api/chat/threads/{threadId}/messages",
            userC.Token);
        await AssertProblemAsync(nonParticipantMessages, 403, "forbidden");

        var markRead = await SendAsync(
            HttpMethod.Patch,
            $"/api/chat/threads/{threadId}/read",
            userB.Token);
        Assert.Equal(HttpStatusCode.NoContent, markRead.StatusCode);

        HttpResponseMessage? directRateLimit = null;
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var response = await SendAsync(
                HttpMethod.Post,
                "/api/chat/threads/direct",
                userA.Token,
                new { otherUserId = userB.UserId });
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                directRateLimit = response;
                break;
            }
        }
        Assert.NotNull(directRateLimit);
        await AssertProblemAsync(directRateLimit, 429, "rate_limit_exceeded");

        HttpResponseMessage? messageRateLimit = null;
        for (var attempt = 0; attempt < 35; attempt++)
        {
            var response = await SendAsync(
                HttpMethod.Post,
                $"/api/chat/threads/{threadId}/messages",
                userA.Token,
                new { body = $"Rate-limited message {attempt}" });
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                messageRateLimit = response;
                break;
            }
        }
        Assert.NotNull(messageRateLimit);
        await AssertProblemAsync(messageRateLimit, 429, "rate_limit_exceeded");

        var threadCount = await Factory.ScalarAsync<long>(
            $"""
             SELECT count(*)
             FROM chat_threads
             WHERE "DirectKey" = (
                 SELECT "DirectKey" FROM chat_threads WHERE "Id" = '{threadId}'
             )
             """);
        Assert.Equal(1, threadCount);

        var messageCount = await Factory.ScalarAsync<long>(
            $"""SELECT count(*) FROM chat_messages WHERE "ThreadId" = '{threadId}'""");
        Assert.Equal(30, messageCount);
    }

    [Fact]
    public async Task Notification_links_are_validated_and_read_state_is_persisted()
    {
        var validator = typeof(CreateNotificationHandler).GetMethod(
            "ValidateAndNormalizeLink",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(validator);

        var unsafeException = Assert.Throws<TargetInvocationException>(
            () => validator.Invoke(null, ["https://evil.example/path"]));
        Assert.IsType<ValidationException>(unsafeException.InnerException);
        Assert.Equal("/notifications/123", validator.Invoke(null, ["/notifications/123"]));

        var user = await CreateUserAsync("Traveler", "notification");
        var notificationId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        await Factory.ExecuteAsync(
            $"""
             INSERT INTO notifications
                 ("Id", "UserId", "Type", "Title", "Body", "LinkUrl",
                  "SourceModule", "SourceEntityType", "SourceEntityId",
                  "CreatedAtUtc", "ReadAtUtc")
             VALUES
                 ('{notificationId}', '{user.UserId}', 'System',
                  'Integration notification', 'Notification body',
                  '/notifications/123', 'Communication', 'Integration',
                  '{sourceId}', NOW(), NULL)
             """);

        var list = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            user.Token);
        list.EnsureSuccessStatusCode();
        using var listJson = await ReadJsonAsync(list);
        Assert.Contains(
            listJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == notificationId);

        var markRead = await SendAsync(
            HttpMethod.Patch,
            $"/api/notifications/{notificationId}/read",
            user.Token);
        markRead.EnsureSuccessStatusCode();

        var readCount = await Factory.ScalarAsync<long>(
            $"""
             SELECT count(*)
             FROM notifications
             WHERE "Id" = '{notificationId}' AND "ReadAtUtc" IS NOT NULL
             """);
        Assert.Equal(1, readCount);
    }
}
