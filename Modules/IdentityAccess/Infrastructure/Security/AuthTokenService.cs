using System.Security.Cryptography;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Security;

public sealed class AuthTokenService : IAuthTokenService
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    private readonly IdentityAccessDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthTokenService(
        IdentityAccessDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> IssueAsync(
        ApplicationUser user,
        IList<string> roles,
        Guid? familyId = null,
        CancellationToken cancellationToken = default)
    {
        var rawToken = CreateToken();
        var expiresAtUtc = DateTime.UtcNow.Add(RefreshLifetime);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = Hash(rawToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return IdentityAccessMappings.ToAuthResponse(
            user,
            roles,
            _jwtTokenGenerator.GenerateToken(user, roles),
            rawToken,
            expiresAtUtc);
    }

    public async Task<AuthResponse> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ValidationException("Refresh token is required.");

        var tokenHash = Hash(refreshToken.Trim());
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        var storedToken = await _dbContext.RefreshTokens
            .FromSqlInterpolated(
                $"""SELECT * FROM refresh_tokens WHERE "TokenHash" = {tokenHash} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

        if (storedToken is null)
            throw new AuthenticationException("Invalid refresh token.");

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString())
                   ?? throw new AuthenticationException("Invalid refresh token.");

        if (storedToken.RevokedAtUtc is not null)
        {
            var now = DateTime.UtcNow;
            storedToken.ReuseDetectedAtUtc ??= now;
            var family = await _dbContext.RefreshTokens
                .Where(x => x.UserId == storedToken.UserId &&
                            x.FamilyId == storedToken.FamilyId &&
                            x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var token in family)
            {
                token.RevokedAtUtc = now;
                token.RevocationReason = "Refresh token reuse detected";
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new AuthenticationException("Refresh token reuse was detected.");
        }

        if (storedToken.ExpiresAtUtc <= DateTime.UtcNow ||
            !user.IsActive ||
            !user.EmailConfirmed)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            storedToken.RevocationReason = "Expired or ineligible user";
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new AuthenticationException("Invalid refresh token.");
        }

        var replacementRaw = CreateToken();
        var replacementHash = Hash(replacementRaw);
        var replacementExpiry = DateTime.UtcNow.Add(RefreshLifetime);
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevocationReason = "Rotated";
        storedToken.ReplacedByTokenHash = replacementHash;
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = storedToken.FamilyId,
            TokenHash = replacementHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = replacementExpiry
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        return IdentityAccessMappings.ToAuthResponse(
            user,
            roles,
            _jwtTokenGenerator.GenerateToken(user, roles),
            replacementRaw,
            replacementExpiry);
    }

    public async Task RevokeAllAsync(
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = reason;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string CreateToken() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
