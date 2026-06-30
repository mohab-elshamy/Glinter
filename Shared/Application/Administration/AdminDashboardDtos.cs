namespace Glinter.Shared.Application.Administration;

public sealed class AdminDashboardResponse
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int PendingBuddyVerifications { get; set; }
    public int ApprovedBuddies { get; set; }
    public int PendingExperiences { get; set; }
    public int ApprovedExperiences { get; set; }
    public int ActiveStays { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalBookingValue { get; set; }
    public int AuditEventsLast24Hours { get; set; }
}

public sealed class AdminAnalyticsResponse
{
    public Dictionary<string, int> UsersByRole { get; set; } = [];
    public Dictionary<string, int> BookingsByStatus { get; set; } = [];
    public Dictionary<string, int> ListingsByType { get; set; } = [];
    public decimal StayBookingValue { get; set; }
    public decimal ExperienceBookingValue { get; set; }
}
