namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class CreateExperienceBookingRequestDto
{
    public Guid AvailabilityId { get; set; }

    public int GuestsCount { get; set; }
}