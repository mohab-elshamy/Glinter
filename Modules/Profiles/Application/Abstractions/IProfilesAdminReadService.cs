namespace Glinter.Modules.Profiles.Application.Abstractions;

public interface IProfilesAdminReadService
{
    Task<ProfilesAdminSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}

public sealed record ProfilesAdminSnapshot(
    int PendingBuddyVerifications,
    int ApprovedBuddies);
