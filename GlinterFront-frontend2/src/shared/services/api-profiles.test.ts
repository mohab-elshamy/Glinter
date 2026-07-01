import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { profilesApi } from "./api-profiles";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("profilesApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("unwraps and continues paged buddy results", async () => {
    const firstPage = Array.from({ length: 50 }, (_, index) => ({
      userId: `buddy-${index}`,
    }));
    vi.mocked(request)
      .mockResolvedValueOnce({
        page: 1,
        pageSize: 50,
        totalCount: 51,
        items: firstPage,
      })
      .mockResolvedValueOnce({
        page: 2,
        pageSize: 50,
        totalCount: 51,
        items: [{ userId: "buddy-50" }],
      });

    const buddies = await profilesApi.getLocalBuddies();

    expect(buddies).toHaveLength(51);
    expect(request).toHaveBeenNthCalledWith(1, "/local-buddies?page=1&pageSize=50");
    expect(request).toHaveBeenNthCalledWith(2, "/local-buddies?page=2&pageSize=50");
  });

  it("uses backend follow state rather than local favorites", async () => {
    vi.mocked(request).mockResolvedValue({ isFollowing: true, followersCount: 4 });
    await profilesApi.followUser("buddy-id");
    expect(request).toHaveBeenCalledWith("/profiles/users/buddy-id/follow", {
      method: "POST",
    });

    await profilesApi.unfollowUser("buddy-id");
    expect(request).toHaveBeenLastCalledWith("/profiles/users/buddy-id/follow", {
      method: "DELETE",
    });
  });

  it("wires buddy availability, booking, and review routes", async () => {
    vi.mocked(request).mockResolvedValue({});

    await profilesApi.getBuddyAvailability("buddy-id");
    expect(request).toHaveBeenLastCalledWith("/local-buddies/buddy-id/availability");

    await profilesApi.createBuddyBooking("buddy-id", "slot-id", "Museum");
    expect(request).toHaveBeenLastCalledWith("/local-buddies/buddy-id/bookings", {
      method: "POST",
      body: { availabilityId: "slot-id", notes: "Museum" },
    });

    await profilesApi.updateBuddyBookingStatus("booking-id", "Accepted");
    expect(request).toHaveBeenLastCalledWith("/buddy-bookings/booking-id/status", {
      method: "PATCH",
      body: { status: "Accepted" },
    });

    await profilesApi.createBuddyReview("buddy-id", {
      bookingId: "booking-id",
      rating: 5,
      reviewText: "Great guide",
    });
    expect(request).toHaveBeenLastCalledWith("/local-buddies/buddy-id/reviews", {
      method: "POST",
      body: {
        bookingId: "booking-id",
        rating: 5,
        reviewText: "Great guide",
      },
    });
  });
});
