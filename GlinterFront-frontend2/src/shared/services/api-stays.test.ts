import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { staysApi } from "./api-stays";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("stays API", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("uses the backend paged listing and owner routes", async () => {
    vi.mocked(request).mockResolvedValueOnce({ page: 1, pageSize: 20, totalCount: 0, items: [] });
    await staysApi.getStays({ search: "Nile", pageSize: 20 });
    expect(request).toHaveBeenCalledWith("/stays?search=Nile&pageSize=20");

    await staysApi.getMyStays();
    expect(request).toHaveBeenLastCalledWith("/stays/mine");
  });

  it("sends the backend image and amenity contract when creating a stay", async () => {
    const payload = {
      name: "Nile Stay",
      price: 500,
      latitude: 30,
      longitude: 31,
      imageLinks: ["https://example.test/stay.jpg"],
      amenities: ["Wi-Fi"],
      bookingPlatforms: [],
    };

    await staysApi.createStay(payload);
    expect(request).toHaveBeenCalledWith("/stays", { method: "POST", body: payload });
  });

  it("wires traveler bookings, owner status changes, and reviews", async () => {
    await staysApi.createBooking(12, {
      checkInDate: "2026-08-01",
      checkOutDate: "2026-08-03",
      guestCount: 2,
    });
    expect(request).toHaveBeenLastCalledWith("/stays/12/bookings", {
      method: "POST",
      body: {
        checkInDate: "2026-08-01",
        checkOutDate: "2026-08-03",
        guestCount: 2,
      },
    });

    await staysApi.updateBookingStatus("booking-id", "Confirmed");
    expect(request).toHaveBeenLastCalledWith("/stay-bookings/booking-id/status", {
      method: "PATCH",
      body: { status: "Confirmed" },
    });

    await staysApi.createReview(12, { rating: 5, reviewText: "Great" });
    expect(request).toHaveBeenLastCalledWith("/stays/12/reviews", {
      method: "POST",
      body: { rating: 5, reviewText: "Great" },
    });
  });
});
