using System.ComponentModel.DataAnnotations;
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
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public string Pcode { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public JsonElement? GeometryGeoJson { get; set; }
}

public class RegionGeometryAccuracyRequest
{
    [Range(0, 100, ErrorMessage = "GeometryAccuracy must be between 0 and 100.")]
    public int GeometryAccuracy { get; set; } = 0;
}

public class RegionByPointRequest
{
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
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
    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Required]
    [StringLength(20)]
    [RegularExpression(@"^\S+$", ErrorMessage = "Pcode cannot contain whitespace.")]
    public string Pcode { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }

    [Url]
    [StringLength(2048)]
    public string? FlagUrl { get; set; }
}

public class UpdateAdm0Request
{
    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }

    [Url]
    [StringLength(2048)]
    public string? FlagUrl { get; set; }
}

public class CreateAdm1Request
{
    [Range(1, int.MaxValue, ErrorMessage = "Adm0Gid must be greater than zero.")]
    public int Adm0Gid { get; set; }

    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Required]
    [StringLength(20)]
    [RegularExpression(@"^\S+$", ErrorMessage = "Pcode cannot contain whitespace.")]
    public string Pcode { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

public class UpdateAdm1Request
{
    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

public class CreateAdm2Request
{
    [Range(1, int.MaxValue, ErrorMessage = "Adm1Gid must be greater than zero.")]
    public int Adm1Gid { get; set; }

    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Required]
    [StringLength(20)]
    [RegularExpression(@"^\S+$", ErrorMessage = "Pcode cannot contain whitespace.")]
    public string Pcode { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

public class UpdateAdm2Request
{
    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

public class CreateAdm3Request
{
    [Range(1, int.MaxValue, ErrorMessage = "Adm2Gid must be greater than zero.")]
    public int Adm2Gid { get; set; }

    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Required]
    [StringLength(20)]
    [RegularExpression(@"^\S+$", ErrorMessage = "Pcode cannot contain whitespace.")]
    public string Pcode { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}

public class UpdateAdm3Request
{
    [Required]
    [StringLength(100)]
    [RegularExpression(@".*\S.*", ErrorMessage = "NameEn is required.")]
    public string NameEn { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameAr { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ImageUrl { get; set; }
}
