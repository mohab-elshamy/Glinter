using System.Security.Claims;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Profiles.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Services;

public class ExperienceProfileResolver : IExperienceProfileResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProfilesDbContext _profilesDbContext;

    public ExperienceProfileResolver(
        IHttpContextAccessor httpContextAccessor,
        IProfilesDbContext profilesDbContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _profilesDbContext = profilesDbContext;
    }

    public async Task<Guid> GetCurrentExperienceProviderProfileIdAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var profile = await _profilesDbContext.ExperienceProviderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile == null)
        {
            throw new NotFoundException("Experience provider profile was not found. Create an experience provider profile first.");
        }

        return profile.Id;
    }

    public async Task<Guid> GetCurrentTravelerProfileIdAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();

        var profile = await _profilesDbContext.TravelerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile == null)
        {
            throw new NotFoundException("Traveler profile was not found. Create a traveler profile first.");
        }

        return profile.Id;
    }

    private Guid GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new AuthenticationException("Authenticated user is required.");
        }

        var userIdValue =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("user_id") ??
            user.FindFirstValue("uid");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new AuthenticationException("User id claim was not found in token.");
        }

        return userId;
    }
}