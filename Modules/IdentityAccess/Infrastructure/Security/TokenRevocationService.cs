using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Security;

public class TokenRevocationService : ITokenRevocationService
{
    private readonly IdentityAccessDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenRevocationService(
        IdentityAccessDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RevokeCurrentTokenAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User
                   ?? throw new AuthenticationException("No authenticated user.");

        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(jti))
            throw new AuthenticationException("Token does not contain jti.");

        var expUnix = user.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (!long.TryParse(expUnix, out var expSeconds))
            throw new AuthenticationException("Token does not contain exp.");

        var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null;

        var alreadyRevoked = await _dbContext.RevokedTokens.AnyAsync(x => x.Jti == jti, cancellationToken);
        if (alreadyRevoked)
            return;

        _dbContext.RevokedTokens.Add(new RevokedToken
        {
            Id = Guid.NewGuid(),
            Jti = jti,
            UserId = userId,
            RevokedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            Reason = reason
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RevokedTokens.AnyAsync(x => x.Jti == jti, cancellationToken);
    }
}