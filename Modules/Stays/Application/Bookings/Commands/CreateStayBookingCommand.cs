namespace Glinter.Modules.Stays.Application.Bookings.Commands;

public class CreateStayBookingCommand
{
    public Guid StayId { get; set; }
    public Guid TravelerProfileId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
}