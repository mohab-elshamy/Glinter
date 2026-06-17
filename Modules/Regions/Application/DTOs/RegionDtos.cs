using System.Text.Json;

namespace Glinter.Modules.Regions.Application.DTOs;

// ──────────────────────────────────────────────
// Response DTOs
// ──────────────────────────────────────────────

public class Adm0Dto
{
    public int Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? FlagUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>GeoJSON object — only populated when geometryAccuracy is greater than 0.</summary>
    public JsonElement? GeometryGeoJson { get; set; }
}

public class Adm1Dto
{
    public int Gid { get; set; }
    public int Adm0Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public JsonElement? GeometryGeoJson { get; set; }
}

public class Adm2Dto
{
    public int Gid { get; set; }
    public int Adm1Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public JsonElement? GeometryGeoJson { get; set; }
}

public class Adm3Dto
{
    public int Gid { get; set; }
    public int Adm2Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public JsonElement? GeometryGeoJson { get; set; }
}

public class RegionGeometryAccuracyRequest
{
    [System.ComponentModel.DataAnnotations.Range(0, 100, ErrorMessage = "GeometryAccuracy must be between 0 and 100.")]
    public int GeometryAccuracy { get; set; } = 0;
}

public class RegionByPointRequest
{
    [System.ComponentModel.DataAnnotations.Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Lat { get; set; }

    [System.ComponentModel.DataAnnotations.Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Lon { get; set; }
}

public class RegionHierarchyGidsDto
{
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
}

public record RegionGeometryGeoJson(int Gid, string? GeoJson);

// ──────────────────────────────────────────────
// Request DTOs (Create / Update)
// ──────────────────────────────────────────────

public class CreateAdm0Request
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? FlagUrl { get; set; }
}

public class UpdateAdm0Request
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
    public string? FlagUrl { get; set; }
}

public class CreateAdm1Request
{
    public int Adm0Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateAdm1Request
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateAdm2Request
{
    public int Adm1Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateAdm2Request
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
}

public class CreateAdm3Request
{
    public int Adm2Gid { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateAdm3Request
{
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? ImageUrl { get; set; }
}
