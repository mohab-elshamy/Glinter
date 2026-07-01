namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface IIdentityUserReadService
{
    Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveUserWithSecurityStampAsync(
        Guid userId,
        string securityStamp,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

}
