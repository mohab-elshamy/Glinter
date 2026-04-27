using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Application.Abstractions;

public interface IIdentityAccessDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}