using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Abstractions;

public interface ILocationCatalogDbContext
{
    DbSet<Country> Countries { get; }
    DbSet<Governorate> Governorates { get; }
    DbSet<District> Districts { get; }
    DbSet<Area> Areas { get; }
    
    DbSet<DistrictSafetySignal> DistrictSafetySignals { get; }
    DbSet<DistrictIndex> DistrictIndices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}