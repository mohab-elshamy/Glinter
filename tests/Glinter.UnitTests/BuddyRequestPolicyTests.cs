using Glinter.Modules.Buddy.Domain;
using Glinter.Modules.Buddy.Domain.Enums;
using Glinter.Shared.Application.Exceptions;

namespace Glinter.UnitTests;

public sealed class BuddyRequestPolicyTests
{
    private static readonly DateTime Now =
        new(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Pending_request_can_be_decided() =>
        BuddyRequestPolicy.EnsureCanDecide(BuddyBookingStatus.Pending);

    [Theory]
    [InlineData(BuddyBookingStatus.Accepted)]
    [InlineData(BuddyBookingStatus.Rejected)]
    [InlineData(BuddyBookingStatus.Cancelled)]
    [InlineData(BuddyBookingStatus.Completed)]
    public void Final_or_answered_request_cannot_be_decided_again(
        BuddyBookingStatus status) =>
        Assert.Throws<ConflictException>(
            () => BuddyRequestPolicy.EnsureCanDecide(status));

    [Theory]
    [InlineData(BuddyBookingStatus.Accepted)]
    [InlineData(BuddyBookingStatus.Completed)]
    public void Ended_accepted_request_can_be_reviewed(
        BuddyBookingStatus status) =>
        BuddyRequestPolicy.EnsureCanReview(
            status, Now.AddMinutes(-1), false, Now);

    [Fact]
    public void Future_request_cannot_be_reviewed() =>
        Assert.Throws<ConflictException>(
            () => BuddyRequestPolicy.EnsureCanReview(
                BuddyBookingStatus.Accepted,
                Now.AddMinutes(1),
                false,
                Now));

    [Theory]
    [InlineData(BuddyBookingStatus.Pending)]
    [InlineData(BuddyBookingStatus.Rejected)]
    [InlineData(BuddyBookingStatus.Cancelled)]
    public void Non_accepted_request_cannot_be_reviewed(
        BuddyBookingStatus status) =>
        Assert.Throws<ConflictException>(
            () => BuddyRequestPolicy.EnsureCanReview(
                status, Now.AddMinutes(-1), false, Now));

    [Fact]
    public void Request_cannot_be_reviewed_twice() =>
        Assert.Throws<ConflictException>(
            () => BuddyRequestPolicy.EnsureCanReview(
                BuddyBookingStatus.Accepted,
                Now.AddMinutes(-1),
                true,
                Now));

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Rating_boundary_is_valid(int rating) =>
        BuddyRequestPolicy.ValidateRating(rating);

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_outside_boundary_is_rejected(int rating) =>
        Assert.Throws<ValidationException>(
            () => BuddyRequestPolicy.ValidateRating(rating));

    [Fact]
    public void Future_date_range_is_valid() =>
        BuddyRequestPolicy.ValidateAvailability(
            Now.AddHours(1), Now.AddHours(2), 0, Now);

    [Fact]
    public void Past_start_is_rejected() =>
        Assert.Throws<ValidationException>(
            () => BuddyRequestPolicy.ValidateAvailability(
                Now.AddMinutes(-1), Now.AddHours(1), 10, Now));

    [Fact]
    public void Reversed_date_range_is_rejected() =>
        Assert.Throws<ValidationException>(
            () => BuddyRequestPolicy.ValidateAvailability(
                Now.AddHours(2), Now.AddHours(1), 10, Now));
}
