using Glinter.Modules.IdentityAccess.Domain.Entities;

namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}