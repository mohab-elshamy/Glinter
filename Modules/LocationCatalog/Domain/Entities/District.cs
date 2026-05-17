namespace Glinter.Modules.LocationCatalog.Domain.Entities;

public class District
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GovernorateId { get; set; }

    public string Pcode { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Governorate Governorate { get; set; } = null!;

    public ICollection<Area> Areas { get; set; } = new List<Area>();
}