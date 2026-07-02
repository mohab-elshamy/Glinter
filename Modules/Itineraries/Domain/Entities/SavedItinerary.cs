namespace Glinter.Modules.Itineraries.Domain.Entities;

public sealed class SavedItinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public decimal? EstimatedTotalCost { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SavedItineraryItem> Items { get; set; } = [];
}
