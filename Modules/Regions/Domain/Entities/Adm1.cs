using NetTopologySuite.Geometries;

namespace Glinter.Modules.Regions.Domain.Entities;

public class Adm1
{
    public int Gid { get; set; }

    public int Adm0Gid { get; set; }

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string Pcode { get; set; } = string.Empty;

    public MultiPolygon? BoundaryGeom { get; set; }

    public string? ImageUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Adm0 Country { get; set; } = null!;

    public ICollection<Adm2> Districts { get; set; } = new List<Adm2>();
}
