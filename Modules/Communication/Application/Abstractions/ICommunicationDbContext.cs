using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface ICommunicationDbContext
{
    DbSet<ChatThread> ChatThreads { get; }

    DbSet<ChatParticipant> ChatParticipants { get; }

    DbSet<ChatMessage> ChatMessages { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<NotificationPreference> NotificationPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
