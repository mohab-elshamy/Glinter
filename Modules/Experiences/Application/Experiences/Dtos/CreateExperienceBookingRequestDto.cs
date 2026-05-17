namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class CreateExperienceBookingRequestDto
{
    public Guid AvailabilityId { get; set; }

    public Guid TravelerProfileId { get; set; }

    public int GuestsCount { get; set; }
}