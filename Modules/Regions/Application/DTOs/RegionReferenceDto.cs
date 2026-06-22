namespace Glinter.Modules.Regions.Application.DTOs;

public sealed class RegionReferenceDto
{
    public int Adm3Gid { get; set; }
    public string? NeighbourhoodNameEn { get; set; }
    public string? NeighbourhoodNameAr { get; set; }

    public int Adm2Gid { get; set; }
    public string DistrictNameEn { get; set; } = string.Empty;
    public string? DistrictNameAr { get; set; }

    public int Adm1Gid { get; set; }
    public string GovernorateNameEn { get; set; } = string.Empty;
    public string? GovernorateNameAr { get; set; }

    public int Adm0Gid { get; set; }
    public string CountryNameEn { get; set; } = string.Empty;
    public string? CountryNameAr { get; set; }
}
