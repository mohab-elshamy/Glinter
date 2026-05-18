namespace Glinter.Modules.LocationCatalog.Domain.Entities;

public class DistrictIndex
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DistrictId { get; set; }

    public District District { get; set; } = null!;

    public double SafetyScore { get; set; }

    public string SafetyLevel { get; set; } = string.Empty;

    public string? SafetyExplanation { get; set; }

    public double? PriceScore { get; set; }

    public string? PriceLevel { get; set; }

    public string? PriceExplanation { get; set; }

    public double? ServicesScore { get; set; }

    public string? ServicesLevel { get; set; }

    public string? ServicesExplanation { get; set; }

    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}