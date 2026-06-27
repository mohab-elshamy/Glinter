using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Commands;
using Glinter.Modules.Communication.Application.Chats.Queries;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Application.Notifications.Queries;
using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.Communication.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Glinter.Modules.Communication.Infrastructure.DependencyInjection;

public static class CommunicationModule
{
    public static IServiceCollection AddCommunicationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddDbContext<CommunicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        var directThreadPermitLimit = configuration.GetValue<int?>(
            "Communication:RateLimiting:DirectThreadPermitLimit") ?? 10;
        var messagePermitLimit = configuration.GetValue<int?>(
            "Communication:RateLimiting:MessagePermitLimit") ?? 30;
        var windowSeconds = configuration.GetValue<int?>(
            "Communication:RateLimiting:WindowSeconds") ?? 60;

        if (directThreadPermitLimit <= 0)
            throw new InvalidOperationException(
                "Communication:RateLimiting:DirectThreadPermitLimit must be greater than zero.");

        if (messagePermitLimit <= 0)
            throw new InvalidOperationException(
                "Communication:RateLimiting:MessagePermitLimit must be greater than zero.");

        if (windowSeconds <= 0)
            throw new InvalidOperationException(
                "Communication:RateLimiting:WindowSeconds must be greater than zero.");

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                CommunicationRateLimitPolicies.DirectThreadCreation,
                httpContext => CreateFixedWindowPartition(
                    httpContext,
                    directThreadPermitLimit,
                    windowSeconds));

            options.AddPolicy(
                CommunicationRateLimitPolicies.MessageSending,
                httpContext => CreateFixedWindowPartition(
                    httpContext,
                    messagePermitLimit,
                    windowSeconds));
        });

        services.AddScoped<ICommunicationDbContext>(provider =>
            provider.GetRequiredService<CommunicationDbContext>());

        services.AddScoped<IChatThreadRepository, ChatThreadRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<CreateDirectChatThreadHandler>();
        services.AddScoped<SendChatMessageHandler>();
        services.AddScoped<MarkChatThreadAsReadHandler>();
        services.AddScoped<GetMyChatThreadsHandler>();
        services.AddScoped<GetChatThreadMessagesHandler>();

        services.AddScoped<CreateNotificationHandler>();
        services.AddScoped<GetMyNotificationsHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();
        services.AddScoped<MarkAllNotificationsAsReadHandler>();

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext httpContext,
        int permitLimit,
        int windowSeconds)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }
}
