using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly CommunicationDbContext _dbContext;

    public NotificationRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification> AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task<Notification?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken);
    }

    public async Task<List<Notification>> GetByUserIdAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .CountAsync(x => x.UserId == userId && x.ReadAtUtc == null, cancellationToken);
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications
            .Where(x => x.UserId == userId && x.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ReadAtUtc, readAtUtc),
                cancellationToken);
    }
}
