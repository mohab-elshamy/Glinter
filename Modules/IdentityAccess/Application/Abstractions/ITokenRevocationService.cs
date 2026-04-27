namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface ITokenRevocationService
{
    Task RevokeCurrentTokenAsync(string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}