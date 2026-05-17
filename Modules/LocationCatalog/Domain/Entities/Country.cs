namespace Glinter.Modules.LocationCatalog.Domain.Entities;

public class Country
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Pcode { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
}