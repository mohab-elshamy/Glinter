using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Repositories;

public class Adm0Repository(RegionsDbContext db) : IAdm0Repository
{
    public async Task<List<Adm0>> GetAllAsync(RegionListQuery query, CancellationToken ct = default)
    {
        var q = db.Adm0.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            q = q.Where(x =>
                x.NameEn.ToLower().Contains(s) ||
                (x.NameAr != null && x.NameAr.ToLower().Contains(s)) ||
                x.Pcode.ToLower().Contains(s));
        }

        q = q.Select(x => new Adm0
        {
            Gid = x.Gid,
            NameEn = x.NameEn,
            NameAr = x.NameAr,
            Pcode = x.Pcode,
            ImageUrl = x.ImageUrl,
            FlagUrl = x.FlagUrl,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
        });

        q = q.OrderBy(x => x.NameEn)
             .Skip((query.Page - 1) * query.PageSize)
             .Take(query.PageSize);

        return await q.ToListAsync(ct);
    }

    public async Task<Adm0?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default)
    {
        if (loadGeometry)
            return await db.Adm0.FirstOrDefaultAsync(x => x.Gid == gid, ct);

        return await db.Adm0
            .Select(x => new Adm0
            {
                Gid = x.Gid,
                NameEn = x.NameEn,
                NameAr = x.NameAr,
                Pcode = x.Pcode,
                ImageUrl = x.ImageUrl,
                FlagUrl = x.FlagUrl,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .FirstOrDefaultAsync(x => x.Gid == gid, ct);
    }

    public Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(
        int gid,
        int geometryAccuracy,
        CancellationToken ct = default)
        => RegionGeometrySql.GetGeometryGeoJsonAsync(db, "adm0", gid, geometryAccuracy, ct);

    public Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(
        IReadOnlyCollection<int> gids,
        int geometryAccuracy,
        CancellationToken ct = default)
        => RegionGeometrySql.GetGeometryGeoJsonByIdsAsync(db, "adm0", gids, geometryAccuracy, ct);

    public async Task<Adm0?> GetByPcodeAsync(string pcode, CancellationToken ct = default)
        => await db.Adm0.FirstOrDefaultAsync(x => x.Pcode == pcode, ct);

    public async Task<Adm0> AddAsync(Adm0 entity, CancellationToken ct = default)
    {
        db.Adm0.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Adm0?> UpdateAsync(Adm0 entity, CancellationToken ct = default)
    {
        db.Adm0.Update(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(int gid, CancellationToken ct = default)
    {
        var entity = await db.Adm0.FindAsync([gid], ct);
        if (entity is null) return false;
        db.Adm0.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
