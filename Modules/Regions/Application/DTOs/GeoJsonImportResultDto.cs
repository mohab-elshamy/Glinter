namespace Glinter.Modules.Regions.Application.DTOs;

public sealed class GeoJsonImportResultDto
{
    public bool Success { get; set; }
    public string Layer { get; set; } = default!;
    public int TotalFeatures { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}
