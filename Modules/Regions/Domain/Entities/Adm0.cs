using NetTopologySuite.Geometries;

namespace Glinter.Modules.Regions.Domain.Entities;

public class Adm0
{
    public int Gid { get; set; }

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string Pcode { get; set; } = string.Empty;

    public MultiPolygon? BoundaryGeom { get; set; }

    public string? ImageUrl { get; set; }

    public string? FlagUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Adm1> Governorates { get; set; } = new List<Adm1>();
}
