using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;

namespace Glinter.Modules.Regions.Application.Neighbourhoods.Commands;

public class CreateNeighbourhoodCommand
{
    public int Adm2Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateNeighbourhoodCommand
{
    public int Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateNeighbourhoodHandler(IAdm3Repository repository)
{
    public async Task<Adm3Dto> HandleAsync(CreateNeighbourhoodCommand command, CancellationToken ct)
    {
        var entity = new Adm3
        {
            Adm2Gid = command.Adm2Gid,
            NameEn = command.NameEn,
            NameAr = command.NameAr,
            Pcode = command.Pcode,
            ImageUrl = command.ImageUrl,
        };
        var created = await repository.AddAsync(entity, ct);
        return NeighbourhoodCommandMapper.Map(created, false);
    }
}

public class UpdateNeighbourhoodHandler(IAdm3Repository repository)
{
    public async Task<Adm3Dto?> HandleAsync(UpdateNeighbourhoodCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        return updated is null ? null : NeighbourhoodCommandMapper.Map(updated, false);
    }
}

public class DeleteNeighbourhoodHandler(IAdm3Repository repository)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
        => await repository.DeleteAsync(gid, ct);
}

internal static class NeighbourhoodCommandMapper
{
    internal static Adm3Dto Map(Adm3 e, bool geo)
    {
        string? geoJson = null;
        if (geo && e.BoundaryGeom is not null)
            geoJson = new GeoJsonWriter().Write(e.BoundaryGeom);

        return new Adm3Dto
        {
            Gid = e.Gid,
            Adm2Gid = e.Adm2Gid,
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
