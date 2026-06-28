namespace Glinter.Shared.Application.Dashboards;

public sealed class RevenueByCurrencyDto
{
    public string Currency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersLast30Days { get; set; }
    public int TotalStays { get; set; }
    public int ActiveStays { get; set; }
    public int StayBookings { get; set; }
    public int TotalExperiences { get; set; }
    public int ApprovedExperiences { get; set; }
    public int PendingExperiences { get; set; }
    public int RejectedExperiences { get; set; }
    public int ExperienceBookings { get; set; }
    public int PendingLocalBuddyVerifications { get; set; }
    public int ChatMessagesLast24Hours { get; set; }
    public int UnreadNotifications { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}

public sealed class HotelOwnerDashboardDto
{
    public Guid ProfileId { get; set; }
    public int TotalStays { get; set; }
    public int ActiveStays { get; set; }
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int UpcomingCheckInsNext30Days { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
    public List<RevenueByCurrencyDto> Revenue { get; set; } = [];
    public DateTime GeneratedAtUtc { get; set; }
}

public sealed class ExperienceProviderDashboardDto
{
    public Guid ProfileId { get; set; }
    public int TotalExperiences { get; set; }
    public int ActiveExperiences { get; set; }
    public int PendingExperiences { get; set; }
    public int ApprovedExperiences { get; set; }
    public int RejectedExperiences { get; set; }
    public int UpcomingAvailabilitySlots { get; set; }
    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int GuestsBooked { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
    public List<RevenueByCurrencyDto> Revenue { get; set; } = [];
    public DateTime GeneratedAtUtc { get; set; }
}
