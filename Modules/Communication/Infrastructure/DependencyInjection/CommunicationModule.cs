using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Commands;
using Glinter.Modules.Communication.Application.Chats.Queries;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Application.Notifications.Queries;
using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.Communication.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

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
}
