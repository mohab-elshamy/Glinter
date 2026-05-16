using System.Security.Claims;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var userIdValue =
                User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub")
                ?? User?.FindFirstValue("userId");

            return Guid.TryParse(userIdValue, out var userId)
                ? userId
                : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue("email");

    public IReadOnlyCollection<string> Roles =>
        User?
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Concat(User.FindAll("role").Select(x => x.Value))
            .Distinct()
            .ToList()
        ?? [];
}