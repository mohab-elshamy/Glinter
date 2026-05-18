namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceResponseDto
{
    public Guid Id { get; set; }

    public Guid ProviderProfileId { get; set; }

    public Guid CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public Guid AreaId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int MaxGuests { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public bool IsActive { get; set; }
    
    public string ApprovalStatus { get; set; } = string.Empty;

    public string? ModerationNotes { get; set; }

    public DateTime? ModeratedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public List<VibeResponseDto> Vibes { get; set; } = new();

    public List<ExperienceTagDto> Tags { get; set; } = new();
}