namespace Glinter.Modules.LocationCatalog.Domain.Entities;

public class Area
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DistrictId { get; set; }

    public string Pcode { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public District District { get; set; } = null!;
}