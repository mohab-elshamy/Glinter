using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;
using Glinter.Shared.Application.Auditing;

namespace Glinter.Modules.Regions.Application.Governorates.Commands;

public class CreateGovernorateCommand
{
    public int Adm0Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateGovernorateCommand
{
    public int Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateGovernorateHandler(
    IAdm1Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<Adm1Dto> HandleAsync(CreateGovernorateCommand command, CancellationToken ct)
    {
        var entity = new Adm1
        {
            Adm0Gid = command.Adm0Gid,
            NameEn = command.NameEn,
            NameAr = command.NameAr,
            Pcode = command.Pcode,
            ImageUrl = command.ImageUrl,
        };
        var created = await repository.AddAsync(entity, ct);
        auditDetails.SetChanges(
            new { Exists = false },
            GovernorateAuditSnapshot.From(created));
        return GovernorateCommandMapper.Map(created, false);
    }
}

public class UpdateGovernorateHandler(
    IAdm1Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<Adm1Dto?> HandleAsync(UpdateGovernorateCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;
        var before = GovernorateAuditSnapshot.From(entity);

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        if (updated is not null)
            auditDetails.SetChanges(before, GovernorateAuditSnapshot.From(updated));
        return updated is null ? null : GovernorateCommandMapper.Map(updated, false);
    }
}

public class DeleteGovernorateHandler(
    IAdm1Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(gid, false, ct);
        if (entity is null)
            return false;

        var before = GovernorateAuditSnapshot.From(entity);
        var deleted = await repository.DeleteAsync(gid, ct);
        if (deleted)
            auditDetails.SetChanges(before, new { Deleted = true });
        return deleted;
    }
}

internal sealed record GovernorateAuditSnapshot(
    int Gid,
    int Adm0Gid,
    string NameEn,
    string? NameAr,
    string Pcode,
    string? ImageUrl)
{
    internal static GovernorateAuditSnapshot From(Adm1 value) =>
        new(
            value.Gid,
            value.Adm0Gid,
            value.NameEn,
            value.NameAr,
            value.Pcode,
            value.ImageUrl);
}

internal static class GovernorateCommandMapper
{
    internal static Adm1Dto Map(Adm1 e, bool geo)
    {
        string? geoJson = null;
        if (geo && e.BoundaryGeom is not null)
            geoJson = new GeoJsonWriter().Write(e.BoundaryGeom);

        return new Adm1Dto
        {
            Gid = e.Gid,
            Adm0Gid = e.Adm0Gid,
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
