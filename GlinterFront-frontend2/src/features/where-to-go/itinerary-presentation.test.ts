import { describe, expect, it } from "vitest";
import { buildItineraryRouteLines, formatBudgetFit } from "./itinerary-presentation";
import type { ItineraryPlanResponse } from "@/shared/types/itineraries";

const plan = {
  budgetApplied: true,
  totalBudget: 100,
  isWithinBudget: true,
  days: [{
    dayNumber: 1,
    legs: [{
      fromOrder: 0,
      toOrder: 1,
      mode: "Walking",
      provider: "Fallback",
      distanceKm: 1.25,
      durationMinutes: 18,
      geometry: { type: "LineString", coordinates: [[31.2, 30.1], [31.3, 30.2]] },
      steps: [],
      warnings: ["Provider unavailable"],
    }],
  }],
} as ItineraryPlanResponse;

describe("itinerary presentation", () => {
  it("converts backend longitude/latitude route geometry for Leaflet", () => {
    expect(buildItineraryRouteLines(plan)).toEqual([expect.objectContaining({
      positions: [[30.1, 31.2], [30.2, 31.3]],
      dashed: true,
    })]);
  });

  it("ignores missing or invalid route geometry without inventing a line", () => {
    const missing = { ...plan, days: [{ ...plan.days[0], legs: [{ ...plan.days[0].legs[0], geometry: undefined }] }] };
    expect(buildItineraryRouteLines(missing)).toEqual([]);
  });

  it("reports backend budget fit", () => {
    expect(formatBudgetFit(plan)).toBe("Within budget");
    expect(formatBudgetFit({ ...plan, isWithinBudget: false })).toBe("Over budget");
  });
});
