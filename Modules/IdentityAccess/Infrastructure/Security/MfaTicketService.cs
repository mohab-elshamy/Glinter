using System.Security.Cryptography;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Security;

public sealed class MfaTicketService : IMfaTicketService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly IdentityAccessDbContext _dbContext;

    public MfaTicketService(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MfaTicket> CreateAsync(
        Guid userId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var cleanupBeforeUtc = DateTime.UtcNow.AddDays(-1);
        await _dbContext.MfaChallenges
            .Where(x => x.UserId == userId &&
                        (x.ExpiresAtUtc < cleanupBeforeUtc ||
                         x.ConsumedAtUtc < cleanupBeforeUtc))
            .ExecuteDeleteAsync(cancellationToken);

        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.Add(Lifetime);
        _dbContext.MfaChallenges.Add(new MfaChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            TokenHash = Hash(value),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new MfaTicket(value, expiresAtUtc);
    }

    public async Task<Guid> ConsumeAsync(
        string ticket,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            throw new ValidationException("MFA ticket is required.");

        var tokenHash = Hash(ticket.Trim());
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        var challenge = await _dbContext.MfaChallenges
            .FromSqlInterpolated(
                $"""SELECT * FROM mfa_challenges WHERE "TokenHash" = {tokenHash} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

        if (challenge is null ||
            challenge.Purpose != purpose ||
            challenge.ConsumedAtUtc is not null ||
            challenge.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AuthenticationException("Invalid or expired MFA ticket.");
        }

        challenge.ConsumedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return challenge.UserId;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
