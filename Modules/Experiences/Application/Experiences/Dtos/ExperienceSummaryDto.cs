namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceSummaryDto
{
    public Guid Id { get; set; }

    public Guid ProviderProfileId { get; set; }

    public Guid CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public int Adm3Gid { get; set; }

    public Glinter.Modules.Regions.Application.DTOs.RegionReferenceDto? Region { get; set; }

    public string Title { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int MaxGuests { get; set; }

    public bool IsActive { get; set; }

    public string ApprovalStatus { get; set; } = string.Empty;

    public string? ModerationNotes { get; set; }

    public List<string> Vibes { get; set; } = new();

    public List<string> Tags { get; set; } = new();
}
