// @vitest-environment jsdom

import { beforeEach, describe, expect, it } from "vitest";
import {
  BUDGET_LEVEL_STORAGE_KEY,
  budgetLevelToProfileString,
  parseBudgetLevel,
  readStoredBudgetLevel,
} from "./budget-levels";

describe("budget levels", () => {
  beforeEach(() => localStorage.clear());

  it.each([
    ["Budget", 1],
    ["Economy", 2],
    ["Mid-range", 3],
    ["MidRange", 3],
    ["Upscale", 4],
    ["Luxury", 5],
    ["$20-30", 1],
    ["$30-50", 3],
    ["$50-80", 4],
    ["$80+", 5],
  ] as const)("maps legacy %s to level %s", (value, expected) => {
    expect(parseBudgetLevel(value)).toBe(expected);
  });

  it("uses canonical profile strings", () => {
    expect([1, 2, 3, 4, 5].map((level) =>
      budgetLevelToProfileString(level as 1 | 2 | 3 | 4 | 5)))
      .toEqual(["Budget", "Economy", "MidRange", "Upscale", "Luxury"]);
  });

  it("migrates the old dashboard storage key", () => {
    localStorage.setItem("budgetRange", "$50-80");
    expect(readStoredBudgetLevel(localStorage)).toBe(4);
    expect(localStorage.getItem(BUDGET_LEVEL_STORAGE_KEY)).toBe("4");
    expect(localStorage.getItem("budgetRange")).toBeNull();
  });
});
