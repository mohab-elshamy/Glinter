namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class UpdateExperienceRequestDto
{
    public Guid CategoryId { get; set; }

    public Guid AreaId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string LocationName { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public string Currency { get; set; } = "EGP";

    public int DurationMinutes { get; set; }

    public int MaxGuests { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<Guid> VibeIds { get; set; } = new();

    public List<string> Tags { get; set; } = new();
}