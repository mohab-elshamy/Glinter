using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.Communication.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Repositories;

public sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly CommunicationDbContext _dbContext;

    public NotificationPreferenceRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationPreference?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _dbContext.NotificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<NotificationPreference> UpsertAsync(
        NotificationPreference preference,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO notification_preferences
                 ("UserId", "InAppEnabled", "EmailEnabled", "PushEnabled",
                  "ChatMessageNotificationsEnabled", "SystemNotificationsEnabled",
                  "UpdatedAtUtc")
             VALUES
                 ({preference.UserId}, {preference.InAppEnabled},
                  {preference.EmailEnabled}, {preference.PushEnabled},
                  {preference.ChatMessageNotificationsEnabled},
                  {preference.SystemNotificationsEnabled},
                  {preference.UpdatedAtUtc})
             ON CONFLICT ("UserId") DO UPDATE SET
                 "InAppEnabled" = EXCLUDED."InAppEnabled",
                 "EmailEnabled" = EXCLUDED."EmailEnabled",
                 "PushEnabled" = EXCLUDED."PushEnabled",
                 "ChatMessageNotificationsEnabled" =
                     EXCLUDED."ChatMessageNotificationsEnabled",
                 "SystemNotificationsEnabled" =
                     EXCLUDED."SystemNotificationsEnabled",
                 "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
             """,
            cancellationToken);

        return await GetAsync(preference.UserId, cancellationToken)
               ?? throw new InvalidOperationException(
                   "Notification preferences were saved but could not be loaded.");
    }

    public async Task<bool> IsInAppEnabledAsync(
        Guid userId,
        NotificationType type,
        CancellationToken cancellationToken = default)
    {
        var preference = await _dbContext.NotificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preference is null)
            return true;

        return preference.InAppEnabled &&
               (type == NotificationType.ChatMessage
                   ? preference.ChatMessageNotificationsEnabled
                   : preference.SystemNotificationsEnabled);
    }
}
