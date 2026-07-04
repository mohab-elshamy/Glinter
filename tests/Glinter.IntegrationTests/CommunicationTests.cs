using System.Reflection;
using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.Communication.Domain.Enums;
using Glinter.Shared.Application.Exceptions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

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

        var threads = await SendAsync(
            HttpMethod.Get,
            "/api/chat/threads",
            userA.Token);
        threads.EnsureSuccessStatusCode();
        using var threadsJson = await ReadJsonAsync(threads);
        var listedThread = Assert.Single(
            threadsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == threadId);
        Assert.Equal(
            $"Integration Traveler",
            listedThread.GetProperty("participantDisplayNames")
                .GetProperty(userB.UserId.ToString())
                .GetString());

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
        var unsupportedException = Assert.Throws<TargetInvocationException>(
            () => validator.Invoke(null, ["/admin"]));
        Assert.IsType<ValidationException>(unsupportedException.InnerException);
        var malformedThreadException = Assert.Throws<TargetInvocationException>(
            () => validator.Invoke(null, ["/messages?thread=not-a-guid"]));
        Assert.IsType<ValidationException>(malformedThreadException.InnerException);
        Assert.Equal(
            "/profile/me?tab=bookings",
            validator.Invoke(null, ["/profile/me?tab=bookings"]));
        var publicProfileLink = $"/profile/{Guid.NewGuid()}";
        Assert.Equal(
            publicProfileLink,
            validator.Invoke(null, [publicProfileLink]));

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
                  '/profile/me?tab=bookings', 'Communication', 'Integration',
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
        Assert.True(listJson.RootElement.GetProperty("totalCount").GetInt32() >= 1);

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

    [Fact]
    public async Task SignalR_delivers_persisted_messages_only_to_thread_participants()
    {
        var userA = await CreateUserAsync("Traveler", "realtime-a");
        var userB = await CreateUserAsync("Traveler", "realtime-b");
        var userC = await CreateUserAsync("Traveler", "realtime-c");

        var threadResponse = await SendAsync(
            HttpMethod.Post,
            "/api/chat/threads/direct",
            userA.Token,
            new { otherUserId = userB.UserId });
        threadResponse.EnsureSuccessStatusCode();
        using var threadJson = await ReadJsonAsync(threadResponse);
        var threadId = threadJson.RootElement.GetProperty("id").GetGuid();

        await using var participantConnection = CreateHubConnection(userB.Token);
        var messageReceived = new TaskCompletionSource<ChatMessageEventDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        participantConnection.On<ChatMessageEventDto>(
            "MessageReceived",
            message => messageReceived.TrySetResult(message));
        var notificationReceived = new TaskCompletionSource<NotificationResponseDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        participantConnection.On<NotificationResponseDto>(
            "NotificationCreated",
            notification => notificationReceived.TrySetResult(notification));

        await participantConnection.StartAsync();
        await participantConnection.InvokeAsync("JoinThread", threadId);

        var messageResponse = await SendAsync(
            HttpMethod.Post,
            $"/api/chat/threads/{threadId}/messages",
            userA.Token,
            new { body = "SignalR integration message" });
        messageResponse.EnsureSuccessStatusCode();
        using var messageJson = await ReadJsonAsync(messageResponse);

        var realtimeMessage = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(
            messageJson.RootElement.GetProperty("id").GetGuid(),
            realtimeMessage.Id);
        Assert.Equal(threadId, realtimeMessage.ThreadId);
        Assert.Equal(userA.UserId, realtimeMessage.SenderUserId);
        Assert.Equal("SignalR integration message", realtimeMessage.Body);
        var realtimeNotification = await notificationReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(NotificationType.ChatMessage, realtimeNotification.Type);
        Assert.Equal($"/messages?thread={threadId}", realtimeNotification.LinkUrl);
        Assert.False(realtimeNotification.IsRead);

        await Factory.ExecuteAsync(
            $"""
             UPDATE chat_participants
             SET "LeftAtUtc" = NOW()
             WHERE "ThreadId" = '{threadId}' AND "UserId" = '{userB.UserId}'
             """);
        var messageAfterLeaving = new TaskCompletionSource<ChatMessageEventDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        participantConnection.On<ChatMessageEventDto>(
            "MessageReceived",
            message => messageAfterLeaving.TrySetResult(message));

        var messageAfterLeavingResponse = await SendAsync(
            HttpMethod.Post,
            $"/api/chat/threads/{threadId}/messages",
            userA.Token,
            new { body = "Message after participant left" });
        messageAfterLeavingResponse.EnsureSuccessStatusCode();
        await Assert.ThrowsAsync<TimeoutException>(
            () => messageAfterLeaving.Task.WaitAsync(TimeSpan.FromSeconds(1)));

        await Factory.ExecuteAsync(
            $"""
             UPDATE chat_participants
             SET "LeftAtUtc" = NULL
             WHERE "ThreadId" = '{threadId}' AND "UserId" = '{userB.UserId}'
             """);

        var adminToken = await GetAdminTokenAsync();
        var deactivate = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/users/{userB.UserId}/status",
            adminToken,
            new { isActive = false });
        deactivate.EnsureSuccessStatusCode();

        var messageAfterDeactivation = new TaskCompletionSource<ChatMessageEventDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        participantConnection.On<ChatMessageEventDto>(
            "MessageReceived",
            message => messageAfterDeactivation.TrySetResult(message));

        var secondMessageResponse = await SendAsync(
            HttpMethod.Post,
            $"/api/chat/threads/{threadId}/messages",
            userA.Token,
            new { body = "Message after participant deactivation" });
        secondMessageResponse.EnsureSuccessStatusCode();

        await Assert.ThrowsAsync<TimeoutException>(
            () => messageAfterDeactivation.Task.WaitAsync(TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAnyAsync<Exception>(
            () => participantConnection.InvokeAsync("JoinThread", threadId));

        await using var outsiderConnection = CreateHubConnection(userC.Token);
        await outsiderConnection.StartAsync();
        var exception = await Assert.ThrowsAsync<HubException>(
            () => outsiderConnection.InvokeAsync("JoinThread", threadId));
        Assert.Contains("not a participant", exception.Message);
    }

    [Fact]
    public async Task SignalR_access_token_query_value_is_never_written_to_logs()
    {
        var marker = $"signalr-secret-{Guid.NewGuid():N}";
        Factory.LogCollector.Clear();

        await Client.GetAsync($"/hubs/chat?id=invalid&access_token={marker}");
        await Task.Delay(100);

        Assert.DoesNotContain(
            Factory.LogCollector.Messages,
            message => message.Contains(marker, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SignalR_stops_delivery_after_the_joined_token_is_revoked()
    {
        var sender = await CreateUserAsync("Traveler", "realtime-revocation-sender");
        var recipient = await CreateUserAsync("Traveler", "realtime-revocation-recipient");

        var threadResponse = await SendAsync(
            HttpMethod.Post,
            "/api/chat/threads/direct",
            sender.Token,
            new { otherUserId = recipient.UserId });
        threadResponse.EnsureSuccessStatusCode();
        using var threadJson = await ReadJsonAsync(threadResponse);
        var threadId = threadJson.RootElement.GetProperty("id").GetGuid();

        await using var connection = CreateHubConnection(recipient.Token);
        var received = new TaskCompletionSource<ChatMessageEventDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ChatMessageEventDto>(
            "MessageReceived",
            message => received.TrySetResult(message));
        await connection.StartAsync();
        await connection.InvokeAsync("JoinThread", threadId);

        var logout = await SendAsync(
            HttpMethod.Post,
            "/api/auth/logout",
            recipient.Token);
        logout.EnsureSuccessStatusCode();

        var message = await SendAsync(
            HttpMethod.Post,
            $"/api/chat/threads/{threadId}/messages",
            sender.Token,
            new { body = "Must not reach a revoked session" });
        message.EnsureSuccessStatusCode();

        await Assert.ThrowsAsync<TimeoutException>(
            () => received.Task.WaitAsync(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task Notification_preferences_are_defaulted_persisted_and_enforced()
    {
        var user = await CreateUserAsync("Traveler", "preferences");

        var defaults = await SendAsync(
            HttpMethod.Get,
            "/api/notifications/preferences",
            user.Token);
        defaults.EnsureSuccessStatusCode();
        using var defaultsJson = await ReadJsonAsync(defaults);
        Assert.True(defaultsJson.RootElement.GetProperty("inAppEnabled").GetBoolean());
        Assert.True(defaultsJson.RootElement
            .GetProperty("chatMessageNotificationsEnabled").GetBoolean());
        Assert.True(defaultsJson.RootElement
            .GetProperty("systemNotificationsEnabled").GetBoolean());

        var concurrentUpdates = await Task.WhenAll(
            SendAsync(
                HttpMethod.Put,
                "/api/notifications/preferences",
                user.Token,
                new
                {
                    inAppEnabled = true,
                    emailEnabled = false,
                    pushEnabled = true,
                    chatMessageNotificationsEnabled = true,
                    systemNotificationsEnabled = false
                }),
            SendAsync(
                HttpMethod.Put,
                "/api/notifications/preferences",
                user.Token,
                new
                {
                    inAppEnabled = false,
                    emailEnabled = true,
                    pushEnabled = false,
                    chatMessageNotificationsEnabled = false,
                    systemNotificationsEnabled = true
                }));
        foreach (var concurrentUpdate in concurrentUpdates)
        {
            concurrentUpdate.EnsureSuccessStatusCode();
            concurrentUpdate.Dispose();
        }

        var update = await SendAsync(
            HttpMethod.Put,
            "/api/notifications/preferences",
            user.Token,
            new
            {
                inAppEnabled = true,
                emailEnabled = true,
                pushEnabled = false,
                chatMessageNotificationsEnabled = false,
                systemNotificationsEnabled = true
            });
        update.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreateNotificationHandler>();
        var chatNotification = await handler.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = user.UserId,
                Type = NotificationType.ChatMessage,
                Title = "Hidden chat notification",
                Body = "This should respect the preference."
            });
        Assert.Null(chatNotification);

        var systemNotification = await handler.HandleAsync(
            new CreateNotificationCommand
            {
                UserId = user.UserId,
                Type = NotificationType.System,
                Title = "Visible system notification",
                Body = "This category remains enabled."
            });
        Assert.NotNull(systemNotification);

        var preferenceCount = await Factory.ScalarAsync<long>(
            $"""SELECT count(*) FROM notification_preferences WHERE "UserId" = '{user.UserId}'""");
        Assert.Equal(1, preferenceCount);
        var notificationCount = await Factory.ScalarAsync<long>(
            $"""SELECT count(*) FROM notifications WHERE "UserId" = '{user.UserId}'""");
        Assert.Equal(1, notificationCount);
    }

    private HubConnection CreateHubConnection(string token) =>
        new HubConnectionBuilder()
            .WithUrl(
                new Uri(Client.BaseAddress!, "/hubs/chat"),
                options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                })
            .Build();
}
