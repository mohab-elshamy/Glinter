using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Communication.Infrastructure.Persistence;

public class CommunicationDbContext : DbContext, ICommunicationDbContext
{
    public CommunicationDbContext(DbContextOptions<CommunicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();

    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CommunicationDbContext).Assembly,
            type => type.Namespace != null &&
                    type.Namespace.StartsWith("Glinter.Modules.Communication.Infrastructure.Persistence.Configurations"));
    }
}
