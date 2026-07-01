import { describe, expect, it } from "vitest";
import type { StayResponseDto } from "@/shared/types/api";
import { toHotel } from "./hooks";

describe("stay detail mapping", () => {
  it("preserves every backend detail needed by hotel cards and the detail view", () => {
    const response = {
      id: 42,
      sourceType: "ThirdParty",
      name: "Nile View",
      price: 125,
      description: "A complete backend description.",
      googleMapsLink: "https://maps.example.test/nile",
      reviews: 20,
      rating: 4.6,
      website: "https://example.test",
      phoneInternational: "+201000000000",
      locationSummaryDescription: "Maadi, Cairo",
      cid: "stay-42",
      adm0Gid: 1,
      adm1Gid: 2,
      adm2Gid: 3,
      adm3Gid: 4,
      latitude: 29.96,
      longitude: 31.25,
      isActive: true,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: "2026-02-01T00:00:00Z",
      amenities: ["Wi-Fi", "Pool"],
      images: [
        { id: 1, link: "https://example.test/one.jpg" },
        { id: 2, link: "https://example.test/two.jpg" },
      ],
      reviewsPerRating: [{ rating: 5, reviewsCount: 12 }],
      bookingPlatforms: [
        { id: 7, name: "Platform", priceWithTax: 140, link: "https://book.example.test" },
      ],
      featuredReviews: [
        {
          id: 9,
          reviewerName: "Traveler",
          rating: 5,
          reviewText: "Excellent",
          platform: "glinter",
        },
      ],
    } satisfies StayResponseDto;

    const hotel = toHotel(response);

    expect(hotel.images).toHaveLength(2);
    expect(hotel.image).toBe("https://example.test/one.jpg");
    expect(hotel.reviewsPerRating).toEqual([{ rating: 5, reviewsCount: 12 }]);
    expect(hotel.featuredReviews[0].reviewText).toBe("Excellent");
    expect(hotel.bookingPlatforms[0].priceWithTax).toBe(140);
    expect(hotel.website).toBe("https://example.test");
    expect(hotel.phoneInternational).toBe("+201000000000");
    expect(hotel.googleMapsLink).toBe("https://maps.example.test/nile");
    expect(hotel.adm3Gid).toBe(4);
  });
});
