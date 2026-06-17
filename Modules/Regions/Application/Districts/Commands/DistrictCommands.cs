using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;

namespace Glinter.Modules.Regions.Application.Districts.Commands;

public class CreateDistrictCommand
{
    public int Adm1Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateDistrictCommand
{
    public int Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateDistrictHandler(IAdm2Repository repository)
{
    public async Task<Adm2Dto> HandleAsync(CreateDistrictCommand command, CancellationToken ct)
    {
        var entity = new Adm2
        {
            Adm1Gid = command.Adm1Gid,
            NameEn = command.NameEn,
            NameAr = command.NameAr,
            Pcode = command.Pcode,
            ImageUrl = command.ImageUrl,
        };
        var created = await repository.AddAsync(entity, ct);
        return DistrictCommandMapper.Map(created, false);
    }
}

public class UpdateDistrictHandler(IAdm2Repository repository)
{
    public async Task<Adm2Dto?> HandleAsync(UpdateDistrictCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        return updated is null ? null : DistrictCommandMapper.Map(updated, false);
    }
}

public class DeleteDistrictHandler(IAdm2Repository repository)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
        => await repository.DeleteAsync(gid, ct);
}

internal static class DistrictCommandMapper
{
    internal static Adm2Dto Map(Adm2 e, bool geo)
    {
        string? geoJson = null;
        if (geo && e.BoundaryGeom is not null)
            geoJson = new GeoJsonWriter().Write(e.BoundaryGeom);

        return new Adm2Dto
        {
            Gid = e.Gid,
            Adm1Gid = e.Adm1Gid,
            NameEn = e.NameEn,
            NameAr = e.NameAr,
            Pcode = e.Pcode,
            ImageUrl = e.ImageUrl,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            GeometryGeoJson = GeoJsonSerializationHelper.ToJsonElement(geoJson),
        };
    }
}
