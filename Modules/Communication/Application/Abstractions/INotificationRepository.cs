using Glinter.Modules.Communication.Domain.Entities;

namespace Glinter.Modules.Communication.Application.Abstractions;

public interface INotificationRepository
{
    Task<Notification> AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<Notification>> GetByUserIdAsync(
        Guid userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken = default);
}
