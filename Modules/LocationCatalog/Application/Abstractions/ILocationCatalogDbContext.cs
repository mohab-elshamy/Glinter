using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Abstractions;

public interface ILocationCatalogDbContext
{
    DbSet<Country> Countries { get; }
    DbSet<Governorate> Governorates { get; }
    DbSet<District> Districts { get; }
    DbSet<Area> Areas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}