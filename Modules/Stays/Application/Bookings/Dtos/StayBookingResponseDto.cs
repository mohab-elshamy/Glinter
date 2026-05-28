namespace Glinter.Modules.Stays.Application.Bookings.Dtos;

public class StayBookingResponseDto
{
    public Guid Id { get; set; }
    public Guid StayId { get; set; }
    public Guid TravelerProfileId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}