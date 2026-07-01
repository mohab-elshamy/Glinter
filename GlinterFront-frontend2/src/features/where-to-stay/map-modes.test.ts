import { describe, expect, it } from "vitest";
import { getComfortScore, getPriceColor, getScoreColor } from "./map-modes";
import type { Hotel } from "./types";

const hotel: Hotel = {
  id: 1,
  name: "Comfort Stay",
  area: "Cairo",
  rating: 4.5,
  reviews: 100,
  price: 80,
  amenities: ["wifi", "parking", "breakfast", "pool", "restaurant"],
  images: [],
  bookingPlatforms: [],
  reviewsPerRating: [],
  featuredReviews: [],
  sourceType: "ThirdParty",
  createdAtUtc: "2026-01-01T00:00:00Z",
};

describe("stay map modes", () => {
  it("builds a bounded comfort score from backend rating, reviews, and amenities", () => {
    expect(getComfortScore(hotel)).toBe(90);
    expect(getComfortScore({ ...hotel, rating: 0, reviews: 0, amenities: [] })).toBe(0);
  });

  it("uses a shared score scale for safety and comfort", () => {
    expect(getScoreColor(85)).toBe("#22c55e");
    expect(getScoreColor(65)).toBe("#84cc16");
    expect(getScoreColor(45)).toBe("#f59e0b");
    expect(getScoreColor(20)).toBe("#ef4444");
    expect(getScoreColor(undefined)).toBe("#64748b");
  });

  it("compares prices with the regional backend average", () => {
    expect(getPriceColor(70, 100)).toBe("#22c55e");
    expect(getPriceColor(90, 100)).toBe("#84cc16");
    expect(getPriceColor(120, 100)).toBe("#f59e0b");
    expect(getPriceColor(140, 100)).toBe("#ef4444");
    expect(getPriceColor(undefined, 100)).toBe("#64748b");
  });
});
