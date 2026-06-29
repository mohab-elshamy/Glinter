using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;
using Glinter.Shared.Application.Auditing;

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

public class CreateNeighbourhoodHandler(
    IAdm3Repository repository,
    AdminAuditDetailsContext auditDetails)
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
        auditDetails.SetChanges(
            new { Exists = false },
            NeighbourhoodAuditSnapshot.From(created));
        return NeighbourhoodCommandMapper.Map(created, false);
    }
}

public class UpdateNeighbourhoodHandler(
    IAdm3Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<Adm3Dto?> HandleAsync(UpdateNeighbourhoodCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;
        var before = NeighbourhoodAuditSnapshot.From(entity);

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        if (updated is not null)
            auditDetails.SetChanges(before, NeighbourhoodAuditSnapshot.From(updated));
        return updated is null ? null : NeighbourhoodCommandMapper.Map(updated, false);
    }
}

public class DeleteNeighbourhoodHandler(
    IAdm3Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(gid, false, ct);
        if (entity is null)
            return false;

        var before = NeighbourhoodAuditSnapshot.From(entity);
        var deleted = await repository.DeleteAsync(gid, ct);
        if (deleted)
            auditDetails.SetChanges(before, new { Deleted = true });
        return deleted;
    }
}

internal sealed record NeighbourhoodAuditSnapshot(
    int Gid,
    int Adm2Gid,
    string? NameEn,
    string? NameAr,
    string Pcode,
    string? ImageUrl)
{
    internal static NeighbourhoodAuditSnapshot From(Adm3 value) =>
        new(
            value.Gid,
            value.Adm2Gid,
            value.NameEn,
            value.NameAr,
            value.Pcode,
            value.ImageUrl);
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
