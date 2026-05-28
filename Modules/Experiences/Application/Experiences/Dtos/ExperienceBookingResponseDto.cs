namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceBookingResponseDto
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public Guid AvailabilityId { get; set; }

    public Guid TravelerProfileId { get; set; }

    public int GuestsCount { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}