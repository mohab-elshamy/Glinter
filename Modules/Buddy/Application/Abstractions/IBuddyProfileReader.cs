namespace Glinter.Modules.Buddy.Application.Abstractions;

public sealed record TravelerProfileSummary(Guid UserId, string DisplayName);

public sealed record LocalBuddyProfileSummary(
    Guid UserId,
    string DisplayName,
    bool IsApproved);

public interface IBuddyProfileReader
{
    Task<TravelerProfileSummary?> GetTravelerAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<LocalBuddyProfileSummary?> GetLocalBuddyAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetTravelerDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetLocalBuddyDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken);

    Task UpdateLocalBuddyReviewSummaryAsync(
        Guid buddyUserId,
        decimal averageRating,
        int reviewsCount,
        CancellationToken cancellationToken);
}
