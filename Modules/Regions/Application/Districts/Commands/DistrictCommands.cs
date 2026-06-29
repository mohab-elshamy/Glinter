using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;
using Glinter.Shared.Application.Auditing;

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

public class CreateDistrictHandler(
    IAdm2Repository repository,
    AdminAuditDetailsContext auditDetails)
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
        auditDetails.SetChanges(
            new { Exists = false },
            DistrictAuditSnapshot.From(created));
        return DistrictCommandMapper.Map(created, false);
    }
}

public class UpdateDistrictHandler(
    IAdm2Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<Adm2Dto?> HandleAsync(UpdateDistrictCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;
        var before = DistrictAuditSnapshot.From(entity);

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        if (updated is not null)
            auditDetails.SetChanges(before, DistrictAuditSnapshot.From(updated));
        return updated is null ? null : DistrictCommandMapper.Map(updated, false);
    }
}

public class DeleteDistrictHandler(
    IAdm2Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(gid, false, ct);
        if (entity is null)
            return false;

        var before = DistrictAuditSnapshot.From(entity);
        var deleted = await repository.DeleteAsync(gid, ct);
        if (deleted)
            auditDetails.SetChanges(before, new { Deleted = true });
        return deleted;
    }
}

internal sealed record DistrictAuditSnapshot(
    int Gid,
    int Adm1Gid,
    string NameEn,
    string? NameAr,
    string Pcode,
    string? ImageUrl)
{
    internal static DistrictAuditSnapshot From(Adm2 value) =>
        new(
            value.Gid,
            value.Adm1Gid,
            value.NameEn,
            value.NameAr,
            value.Pcode,
            value.ImageUrl);
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
