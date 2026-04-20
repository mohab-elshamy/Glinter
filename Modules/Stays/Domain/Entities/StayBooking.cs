namespace Glinter.Modules.Stays.Domain.Entities;

public class StayBooking
{
    public Guid Id { get; set; }

    public Guid StayId { get; set; }
    public Stay Stay { get; set; } = null!;

    public Guid TravelerProfileId { get; set; }

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    public int GuestCount { get; set; }
    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}