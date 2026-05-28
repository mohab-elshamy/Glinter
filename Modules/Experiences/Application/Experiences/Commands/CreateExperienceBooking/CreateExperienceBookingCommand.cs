namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;

public class CreateExperienceBookingCommand
{
    public Guid ExperienceId { get; set; }

    public Guid AvailabilityId { get; set; }

    public int GuestsCount { get; set; }
}