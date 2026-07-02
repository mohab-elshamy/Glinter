namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperiencesAdminReadService
{
    Task<ExperiencesAdminSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ExperiencesAdminSnapshot(
    int PendingExperiences,
    int ApprovedExperiences,
    int TotalExperiences,
    int BookingCount,
    decimal BookingValue,
    IReadOnlyList<string> BookingStatuses);
