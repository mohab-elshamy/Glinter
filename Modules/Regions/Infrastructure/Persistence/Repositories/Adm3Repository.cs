using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;

public class Adm3Repository(RegionsDbContext db) : IAdm3Repository
{
    private IQueryable<Adm3> BuildQuery(RegionListQuery query)
    {
        var q = db.Adm3.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(x =>
                (x.NameEn != null && x.NameEn.ToLower().Contains(s)) ||
                (x.NameAr != null && x.NameAr.ToLower().Contains(s)) ||
                x.Pcode.ToLower().Contains(s));
        }
        return q;
    }

    private IQueryable<Adm3> StripGeometry(IQueryable<Adm3> q) =>
        q.Select(x => new Adm3
        {
            Gid = x.Gid,
            Adm2Gid = x.Adm2Gid,
            NameEn = x.NameEn,
            NameAr = x.NameAr,
            Pcode = x.Pcode,
            ImageUrl = x.ImageUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
        });

    public async Task<List<Adm3>> GetAllAsync(RegionListQuery query, CancellationToken ct = default)
    {
        var q = BuildQuery(query);
        q = StripGeometry(q);
        return await q.OrderBy(x => x.NameEn)
                      .ThenBy(x => x.Gid)
                      .Skip((query.NormalizedPage - 1) * query.NormalizedPageSize)
                      .Take(query.NormalizedPageSize)
                      .ToListAsync(ct);
    }

    public async Task<List<Adm3>> GetByAdm2Async(int adm2Gid, RegionListQuery query, CancellationToken ct = default)
    {
        var q = BuildQuery(query).Where(x => x.Adm2Gid == adm2Gid);
        q = StripGeometry(q);
        return await q.OrderBy(x => x.NameEn)
                      .ThenBy(x => x.Gid)
                      .Skip((query.NormalizedPage - 1) * query.NormalizedPageSize)
                      .Take(query.NormalizedPageSize)
                      .ToListAsync(ct);
    }

    public async Task<Adm3?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default)
    {
        if (loadGeometry)
            return await db.Adm3.FirstOrDefaultAsync(x => x.Gid == gid, ct);

        return await db.Adm3
            .Select(x => new Adm3
            {
                Gid = x.Gid,
                Adm2Gid = x.Adm2Gid,
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
        => RegionGeometrySql.GetGeometryGeoJsonAsync(db, "adm3", gid, geometryAccuracy, ct);

    public Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(
        IReadOnlyCollection<int> gids,
        int geometryAccuracy,
        CancellationToken ct = default)
        => RegionGeometrySql.GetGeometryGeoJsonByIdsAsync(db, "adm3", gids, geometryAccuracy, ct);

    public async Task<Adm3?> GetByPcodeAsync(string pcode, CancellationToken ct = default)
        => await db.Adm3.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);

    public async Task<Adm3> AddAsync(Adm3 entity, CancellationToken ct = default)
    {
        db.Adm3.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Adm3?> UpdateAsync(Adm3 entity, CancellationToken ct = default)
    {
        db.Adm3.Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(int gid, CancellationToken ct = default)
    {
        var entity = await db.Adm3.FindAsync([gid], ct);
        if (entity is null) return false;
        db.Adm3.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
