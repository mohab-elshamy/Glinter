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
    await staysApi.getStays({
      search: "Nile",
      sortBy: "Price",
      sortDirection: "Asc",
      pageSize: 20,
    });
    expect(request).toHaveBeenCalledWith(
      "/stays?search=Nile&sortBy=Price&sortDirection=Asc&pageSize=20",
    );

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

    await staysApi.getReviews(12, 3, 5);
    expect(request).toHaveBeenLastCalledWith("/stays/12/reviews?page=3&pageSize=5");
  });

  it("wires region statistics and multipart image/import uploads", async () => {
    vi.mocked(request).mockResolvedValue({});

    await staysApi.getRegionStats({
      groupBy: "Adm2",
      adm1Gid: 7,
      maxPrice: 250,
    });
    expect(request).toHaveBeenLastCalledWith(
      "/stays/region-stats?groupBy=Adm2&adm1Gid=7&maxPrice=250",
    );

    const image = new File(["image"], "hotel.png", { type: "image/png" });
    await staysApi.uploadImage(image);
    expect(request).toHaveBeenLastCalledWith(
      "/stays/images",
      expect.objectContaining({ method: "POST", body: expect.any(FormData) }),
    );

    const json = new File(["[]"], "stays.json", { type: "application/json" });
    await staysApi.importStays(json);
    expect(request).toHaveBeenLastCalledWith(
      "/stays/import",
      expect.objectContaining({ method: "POST", body: expect.any(FormData) }),
    );
  });

  it("posts structured and natural-language recommendation bodies to their backend routes", async () => {
    const structured = {
      budgetLevel: 3,
      experienceCategories: [{ category: "Historical", weight: 1 }],
      requestedAmenities: ["WiFi"],
      adm1Gid: 12,
      limit: 5,
      preferredLanguage: "en",
    };
    await staysApi.getRecommendations(structured);
    expect(request).toHaveBeenLastCalledWith("/stays/recommendations", {
      method: "POST",
      body: structured,
    });

    const natural = {
      text: "A quiet hotel near museums",
      adm1Gid: 12,
      limit: 5,
      preferredLanguage: "en",
    };
    await staysApi.getNaturalLanguageRecommendations(natural);
    expect(request).toHaveBeenLastCalledWith(
      "/stays/recommendations/natural-language",
      { method: "POST", body: natural },
    );
  });
});
