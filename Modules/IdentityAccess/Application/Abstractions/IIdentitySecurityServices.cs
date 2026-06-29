using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Entities;

namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface IAuthTokenService
{
    Task<AuthResponse> IssueAsync(
        ApplicationUser user,
        IList<string> roles,
        Guid? familyId = null,
        CancellationToken cancellationToken = default);

    Task<AuthResponse> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);
}

public interface IIdentityEmailSender
{
    Task SendConfirmationAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken = default);
}

public interface IMfaTicketService
{
    Task<MfaTicket> CreateAsync(
        Guid userId,
        string purpose,
        CancellationToken cancellationToken = default);

    Task<Guid> ConsumeAsync(
        string ticket,
        string purpose,
        CancellationToken cancellationToken = default);
}

public sealed record MfaTicket(string Value, DateTime ExpiresAtUtc);
