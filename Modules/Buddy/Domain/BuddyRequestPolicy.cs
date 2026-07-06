using Glinter.Modules.Buddy.Domain.Enums;

namespace Glinter.Modules.Buddy.Domain;

public static class BuddyRequestPolicy
{
    public static void ValidateAvailability(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        decimal price,
        DateTime nowUtc)
    {
        if (startTimeUtc.Kind == DateTimeKind.Unspecified ||
            endTimeUtc.Kind == DateTimeKind.Unspecified)
            throw new ValidationException(
                "Availability times must include a UTC offset.");
        if (startTimeUtc <= nowUtc)
            throw new ValidationException(
                "Availability must start in the future.");
        if (endTimeUtc <= startTimeUtc)
            throw new ValidationException(
                "Availability end time must be after the start time.");
        if (price < 0)
            throw new ValidationException("Price cannot be negative.");
    }

    public static void EnsureCanDecide(BuddyBookingStatus status)
    {
        if (status != BuddyBookingStatus.Pending)
            throw new ConflictException("This buddy request is already final.");
    }

    public static void EnsureCanReview(
        BuddyBookingStatus status,
        DateTime endTimeUtc,
        bool hasReview,
        DateTime nowUtc)
    {
        if (status is not (
                BuddyBookingStatus.Accepted or BuddyBookingStatus.Completed))
            throw new ConflictException(
                "Only an accepted buddy request can be reviewed.");
        if (endTimeUtc > nowUtc)
            throw new ConflictException(
                "This buddy request cannot be reviewed before it ends.");
        if (hasReview)
            throw new ConflictException(
                "This buddy request has already been reviewed.");
    }

    public static void ValidateRating(int rating)
    {
        if (rating is < 1 or > 5)
            throw new ValidationException("Rating must be between 1 and 5.");
    }
}
