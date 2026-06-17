using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using NetTopologySuite.IO;

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

public class CreateGovernorateHandler(IAdm1Repository repository)
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
        return GovernorateCommandMapper.Map(created, false);
    }
}

public class UpdateGovernorateHandler(IAdm1Repository repository)
{
    public async Task<Adm1Dto?> HandleAsync(UpdateGovernorateCommand command, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(command.Gid, false, ct);
        if (entity is null) return null;

        entity.NameEn = command.NameEn;
        entity.NameAr = command.NameAr;
        entity.ImageUrl = command.ImageUrl;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        var updated = await repository.UpdateAsync(entity, ct);
        return updated is null ? null : GovernorateCommandMapper.Map(updated, false);
    }
}

public class DeleteGovernorateHandler(IAdm1Repository repository)
{
    public async Task<bool> HandleAsync(int gid, CancellationToken ct)
        => await repository.DeleteAsync(gid, ct);
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
