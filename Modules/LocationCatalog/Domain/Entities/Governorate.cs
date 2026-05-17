namespace Glinter.Modules.LocationCatalog.Domain.Entities;

public class Governorate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CountryId { get; set; }

    public string Pcode { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Country Country { get; set; } = null!;

    public ICollection<District> Districts { get; set; } = new List<District>();
}