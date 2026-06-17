using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;

public class Adm1Repository(RegionsDbContext db) : IAdm1Repository
{
    private IQueryable<Adm1> BuildQuery(RegionListQuery query)
    {
        var q = db.Adm1.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            q = q.Where(x =>
                x.NameEn.ToLower().Contains(s) ||
                (x.NameAr != null && x.NameAr.ToLower().Contains(s)) ||
                x.Pcode.ToLower().Contains(s));
        }

        return q;
    }

    private IQueryable<Adm1> StripGeometry(IQueryable<Adm1> q) =>
        q.Select(x => new Adm1
        {
            Gid = x.Gid,
            Adm0Gid = x.Adm0Gid,
            NameEn = x.NameEn,
            NameAr = x.NameAr,
            Pcode = x.Pcode,
            ImageUrl = x.ImageUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
        });

    public async Task<List<Adm1>> GetAllAsync(RegionListQuery query, CancellationToken ct = default)
    {
        var q = BuildQuery(query);
        q = StripGeometry(q);
        return await q.OrderBy(x => x.NameEn)
                      .Skip((query.Page - 1) * query.PageSize)
                      .Take(query.PageSize)
                      .ToListAsync(ct);
    }

    public async Task<List<Adm1>> GetByAdm0Async(int adm0Gid, RegionListQuery query, CancellationToken ct = default)
    {
        var q = BuildQuery(query).Where(x => x.Adm0Gid == adm0Gid);
        q = StripGeometry(q);
        return await q.OrderBy(x => x.NameEn)
                      .Skip((query.Page - 1) * query.PageSize)
                      .Take(query.PageSize)
                      .ToListAsync(ct);
    }

    public async Task<Adm1?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default)
    {
        if (loadGeometry)
            return await db.Adm1.FirstOrDefaultAsync(x => x.Gid == gid, ct);

        return await db.Adm1
            .Select(x => new Adm1
            {
                Gid = x.Gid,
                Adm0Gid = x.Adm0Gid,
                NameEn = x.NameEn,
                NameAr = x.NameAr,
                Pcode = x.Pcode,
                ImageUrl = x.ImageUrl,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .FirstOrDefaultAsync(x => x.Gid == gid, ct);
    }

    public Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(
        int gid,
        int geometryAccuracy,
        CancellationToken ct = default)
        => RegionGeometrySql.GetGeometryGeoJsonAsync(db, "adm1", gid, geometryAccuracy, ct);

    public Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(
        IReadOnlyCollection<int> gids,
        int geometryAccuracy,
        CancellationToken ct = default)
        => RegionGeometrySql.GetGeometryGeoJsonByIdsAsync(db, "adm1", gids, geometryAccuracy, ct);

    public async Task<Adm1?> GetByPcodeAsync(string pcode, CancellationToken ct = default)
        => await db.Adm1.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);

    public async Task<Adm1> AddAsync(Adm1 entity, CancellationToken ct = default)
    {
        db.Adm1.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Adm1?> UpdateAsync(Adm1 entity, CancellationToken ct = default)
    {
        db.Adm1.Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(int gid, CancellationToken ct = default)
    {
        var entity = await db.Adm1.FindAsync([gid], ct);
        if (entity is null) return false;
        db.Adm1.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
