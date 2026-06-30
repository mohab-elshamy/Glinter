import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { experiencesApi } from "./api-experiences";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("experiences API", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("uses paged public listings, details, and provider routes", async () => {
    await experiencesApi.getExperiences({ category: "Historical", pageSize: 20 });
    expect(request).toHaveBeenLastCalledWith("/experiences?category=Historical&pageSize=20");

    await experiencesApi.getExperienceById(14);
    expect(request).toHaveBeenLastCalledWith("/experiences/14");

    await experiencesApi.getMyExperiences();
    expect(request).toHaveBeenLastCalledWith("/experiences/my");
  });

  it("wires availability and capacity-aware booking routes", async () => {
    const slot = {
      startTimeUtc: "2026-08-01T10:00:00.000Z",
      endTimeUtc: "2026-08-01T12:00:00.000Z",
      capacity: 8,
      pricePerPerson: 25,
    };
    await experiencesApi.createAvailability(14, slot);
    expect(request).toHaveBeenLastCalledWith("/experiences/14/availability", {
      method: "POST",
      body: slot,
    });

    await experiencesApi.createBooking(14, { availabilityId: "slot-id", guestsCount: 2 });
    expect(request).toHaveBeenLastCalledWith("/experiences/14/bookings", {
      method: "POST",
      body: { availabilityId: "slot-id", guestsCount: 2 },
    });
  });

  it("wires provider booking status and traveler review routes", async () => {
    await experiencesApi.updateBookingStatus("booking-id", "Confirmed");
    expect(request).toHaveBeenLastCalledWith("/experience-bookings/booking-id/status", {
      method: "PATCH",
      body: { status: "Confirmed" },
    });

    await experiencesApi.createReview(14, { rating: 5, reviewText: "Excellent" });
    expect(request).toHaveBeenLastCalledWith("/experiences/14/reviews", {
      method: "POST",
      body: { rating: 5, reviewText: "Excellent" },
    });
  });
});
