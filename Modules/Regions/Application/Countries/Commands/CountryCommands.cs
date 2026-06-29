using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;
using System.Text;
using Glinter.Shared.Application.Auditing;

namespace Glinter.Modules.Regions.Application.Countries.Commands;

// ──────── Commands ────────

public class CreateCountryCommand
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? FlagUrl { get; set; }
}

public class UpdateCountryCommand
{
    public int Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
    public string? FlagUrl { get; set; }
}

// ──────── Handlers ────────

public class CreateCountryHandler(
    IAdm0Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<Adm0Dto> HandleAsync(CreateCountryCommand command, CancellationToken ct)
    {
        var entity = new Adm0
        {
            NameEn = command.NameEn,
            NameAr = command.NameAr,
            Pcode = command.Pcode,
            ImageUrl = command.ImageUrl,
            FlagUrl = command.FlagUrl,
        };

        var created = await repository.AddAsync(entity, ct);
        auditDetails.SetChanges(
            new { Exists = false },
            CountryAuditSnapshot.From(created));
        return CountryMapper.MapToDto(created, false);
    }
}

public class UpdateCountryHandler(
    IAdm0Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<Adm0Dto?> HandleAsync(UpdateCountryCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;
        var before = CountryAuditSnapshot.From(entity);

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.FlagUrl = command.FlagUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        if (updated is not null)
            auditDetails.SetChanges(before, CountryAuditSnapshot.From(updated));
        return updated is null ? null : CountryMapper.MapToDto(updated, false);
    }
}

public class DeleteCountryHandler(
    IAdm0Repository repository,
    AdminAuditDetailsContext auditDetails)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(gid, false, ct);
        if (entity is null)
            return false;

        var before = CountryAuditSnapshot.From(entity);
        var deleted = await repository.DeleteAsync(gid, ct);
        if (deleted)
            auditDetails.SetChanges(before, new { Deleted = true });
        return deleted;
    }
}

internal sealed record CountryAuditSnapshot(
    int Gid,
    string NameEn,
    string? NameAr,
    string Pcode,
    string? ImageUrl,
    string? FlagUrl)
{
    internal static CountryAuditSnapshot From(Adm0 value) =>
        new(
            value.Gid,
            value.NameEn,
            value.NameAr,
            value.Pcode,
            value.ImageUrl,
            value.FlagUrl);
}

// ──────── Shared mapper ────────

public static class CountryMapper
{
    public static Adm0Dto MapToDto(Adm0 entity, bool loadGeometry)
    {
        string? geoJson = null;
        if (loadGeometry && entity.BoundaryGeom is not null)
            geoJson = new GeoJsonWriter().Write(entity.BoundaryGeom);

        return new Adm0Dto
        {
            Gid = entity.Gid,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Pcode = entity.Pcode,
            ImageUrl = entity.ImageUrl,
            FlagUrl = entity.FlagUrl,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            GeometryGeoJson = GeoJsonSerializationHelper.ToJsonElement(geoJson),
        };
    }
}
