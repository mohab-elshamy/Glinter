import { describe, expect, it } from "vitest";
import type { ExperienceSummaryDto } from "@/shared/types/api";
import {
  buildFrontendItinerary,
  calculateDistanceKm,
  calculateKnownExperienceCost,
  enumerateTripDates,
  moveItineraryItem,
} from "./planner";

const experience = (
  id: number,
  price: number | undefined,
  category: ExperienceSummaryDto["category"] = "Historical",
): ExperienceSummaryDto => ({
  id,
  category,
  sourceType: "ThirdParty",
  name: `Experience ${id}`,
  featuredImages: [],
  hours: [],
  popularTimes: [],
  startingPricePerPerson: price,
  reviewsPerRating: [],
  amenities: [],
  featuredReviews: [],
  isActive: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  moderationStatus: "Approved",
});

describe("frontend itinerary planner", () => {
  it("enumerates inclusive dates and caps long trips", () => {
    expect(enumerateTripDates("2026-07-01", "2026-07-03")).toEqual([
      "2026-07-01",
      "2026-07-02",
      "2026-07-03",
    ]);
    expect(enumerateTripDates("2026-07-01", "2026-08-01")).toHaveLength(14);
  });

  it("builds from selected interests and stays within the known budget", () => {
    const result = buildFrontendItinerary(
      [experience(1, 30), experience(2, 45, "Nature"), experience(3, undefined, "Nature")],
      {
        startDate: "2026-07-01",
        endDate: "2026-07-02",
        budget: 50,
        interests: ["Nature"],
        activityLevel: "Balanced",
      },
    );

    expect(result.map((item) => item.experience.id)).toEqual([2, 3]);
    expect(calculateKnownExperienceCost(result)).toBe(45);
  });

  it("reorders activities only within the same day", () => {
    const items = [
      { experience: experience(1, 10), date: "2026-07-01", time: "09:00" },
      { experience: experience(2, 20), date: "2026-07-01", time: "12:00" },
      { experience: experience(3, 30), date: "2026-07-02", time: "09:00" },
    ];

    expect(moveItineraryItem(items, 2, -1).map((item) => item.experience.id)).toEqual([2, 1, 3]);
    expect(moveItineraryItem(items, 2, 1)).toBe(items);
  });

  it("calculates distance and can prioritize nearby mapped experiences", () => {
    const nearby = {
      ...experience(1, 20),
      latitude: 30.05,
      longitude: 31.24,
      rating: 3,
    };
    const farther = {
      ...experience(2, 20),
      latitude: 25.69,
      longitude: 32.64,
      rating: 5,
    };
    const cairo = { latitude: 30.0444, longitude: 31.2357 };

    expect(calculateDistanceKm(cairo, nearby)).toBeLessThan(1);
    expect(calculateDistanceKm(cairo, experience(3, 20))).toBeUndefined();
    const result = buildFrontendItinerary([farther, nearby], {
      startDate: "2026-07-01",
      endDate: "2026-07-01",
      interests: [],
      activityLevel: "Relaxed",
      prioritizeNearest: true,
      currentPosition: cairo,
    });
    expect(result[0].experience.id).toBe(1);
  });
});
