namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CompleteExperienceBooking;

public class CompleteExperienceBookingCommandValidator
{
    public void Validate(CompleteExperienceBookingCommand command)
    {
        if (command.BookingId == Guid.Empty)
        {
            throw new ArgumentException("BookingId is required.");
        }
    }
}