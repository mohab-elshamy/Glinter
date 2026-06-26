namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface IIdentityUserReadService
{
    Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
