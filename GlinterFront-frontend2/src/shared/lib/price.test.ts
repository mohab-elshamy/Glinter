import { describe, expect, it } from "vitest";
import {
  formatExperiencePrice,
  formatUsdPrice,
  isFreeExperience,
} from "./price";
import { computePrice } from "@/features/where-to-stay/pricing";

describe("backend-derived pricing", () => {
  it("formats free and unavailable prices explicitly", () => {
    expect(formatUsdPrice(0)).toBe("Free");
    expect(formatUsdPrice(undefined)).toBe("Price unavailable");
    expect(formatUsdPrice(24.5)).toBe("$24.5");
  });

  it("prefers an experience's active-slot starting price over its text range", () => {
    expect(formatExperiencePrice(15, "$20–$40")).toBe("From $15/person");
    expect(formatExperiencePrice(0, "$20–$40")).toBe("Free");
    expect(formatExperiencePrice(undefined, "$20–$40")).toBe("$20–$40");
    expect(formatExperiencePrice(undefined, undefined)).toBe("Price unavailable");
  });

  it("recognizes free experiences without treating missing prices as free", () => {
    expect(isFreeExperience(0, undefined)).toBe(true);
    expect(isFreeExperience(undefined, "Free")).toBe(true);
    expect(isFreeExperience(undefined, "$0")).toBe(true);
    expect(isFreeExperience(undefined, undefined)).toBe(false);
  });

  it("uses the backend nightly price without invented adjustments", () => {
    expect(computePrice(125, "2026-12-01", "2026-12-04")).toEqual({
      nightlyPrice: 125,
      nights: 3,
      total: 375,
    });
    expect(computePrice(undefined, "2026-12-01", "2026-12-04").total).toBeNull();
  });
});
