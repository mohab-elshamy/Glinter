namespace Glinter.Modules.Stays.Application.Bookings.Dtos;

public class CreateStayBookingRequestDto
{
    public Guid TravelerProfileId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
}