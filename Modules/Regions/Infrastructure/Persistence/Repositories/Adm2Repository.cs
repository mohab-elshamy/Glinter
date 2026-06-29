using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;

public class Adm2Repository(RegionsDbContext db) : IAdm2Repository
{
    private IQueryable<Adm2> BuildQuery(RegionListQuery query)
    {
        var q = db.Adm2.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(x =>
                x.NameEn.ToLower().Contains(s) ||
                (x.NameAr != null && x.NameAr.ToLower().Contains(s)) ||
                x.Pcode.ToLower().Contains(s));
        }
        return q;
    }

    private IQueryable<Adm2> StripGeometry(IQueryable<Adm2> q) =>
        q.Select(x => new Adm2
        {
            Gid = x.Gid,
            Adm1Gid = x.Adm1Gid,
            NameEn = x.NameEn,
            NameAr = x.NameAr,
            Pcode = x.Pcode,
            ImageUrl = x.ImageUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
        });

    public async Task<List<Adm2>> GetAllAsync(RegionListQuery query, CancellationToken ct = default)
    {
        var q = BuildQuery(query);
        q = StripGeometry(q);
        return await q.OrderBy(x => x.NameEn)
                      .ThenBy(x => x.Gid)
                      .Skip((query.NormalizedPage - 1) * query.NormalizedPageSize)
                      .Take(query.NormalizedPageSize)
                      .ToListAsync(ct);
    }

    public async Task<List<Adm2>> GetByAdm1Async(int adm1Gid, RegionListQuery query, CancellationToken ct = default)
    {
        var q = BuildQuery(query).Where(x => x.Adm1Gid == adm1Gid);
        q = StripGeometry(q);
        return await q.OrderBy(x => x.NameEn)
                      .ThenBy(x => x.Gid)
                      .Skip((query.NormalizedPage - 1) * query.NormalizedPageSize)
                      .Take(query.NormalizedPageSize)
                      .ToListAsync(ct);
    }

    public async Task<Adm2?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default)
    {
        if (loadGeometry)
            return await db.Adm2.FirstOrDefaultAsync(x => x.Gid == gid, ct);

        return await db.Adm2
            .Select(x => new Adm2
            {
                Gid = x.Gid,
                Adm1Gid = x.Adm1Gid,
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
        => RegionGeometrySql.GetGeometryGeoJsonAsync(db, "adm2", gid, geometryAccuracy, ct);

    public Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(
        IReadOnlyCollection<int> gids,
        int geometryAccuracy,
        CancellationToken ct = default)
        => RegionGeometrySql.GetGeometryGeoJsonByIdsAsync(db, "adm2", gids, geometryAccuracy, ct);

    public async Task<Adm2?> GetByPcodeAsync(string pcode, CancellationToken ct = default)
        => await db.Adm2.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);

    public async Task<Adm2> AddAsync(Adm2 entity, CancellationToken ct = default)
    {
        db.Adm2.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Adm2?> UpdateAsync(Adm2 entity, CancellationToken ct = default)
    {
        db.Adm2.Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(int gid, CancellationToken ct = default)
    {
        var entity = await db.Adm2.FindAsync([gid], ct);
        if (entity is null) return false;
        db.Adm2.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
