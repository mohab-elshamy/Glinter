namespace Glinter.Modules.Stays.Application.Abstractions;

public interface IStaysAdminReadService
{
    Task<StaysAdminSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}

public sealed record StaysAdminSnapshot(
    int ActiveStays,
    int TotalStays,
    int BookingCount,
    decimal BookingValue,
    IReadOnlyList<string> BookingStatuses);
