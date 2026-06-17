using NetTopologySuite.Geometries;

namespace Glinter.Modules.Regions.Domain.Entities;

public class Adm3
{
    public int Gid { get; set; }

    public int Adm2Gid { get; set; }

    public string? NameEn { get; set; }

    public string? NameAr { get; set; }

    public string Pcode { get; set; } = string.Empty;

    public MultiPolygon? BoundaryGeom { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Adm2 District { get; set; } = null!;
}
