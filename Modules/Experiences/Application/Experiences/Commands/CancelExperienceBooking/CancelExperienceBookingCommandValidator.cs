namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CancelExperienceBooking;

public class CancelExperienceBookingCommandValidator
{
    public void Validate(CancelExperienceBookingCommand command)
    {
        if (command.BookingId == Guid.Empty)
        {
            throw new ValidationException("BookingId is required.");
        }
    }
}