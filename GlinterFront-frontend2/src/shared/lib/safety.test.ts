import { describe, expect, it } from "vitest";
import { getSafetyDataState } from "./safety";

const now = new Date("2026-06-30T12:00:00Z");

describe("getSafetyDataState", () => {
  it("marks a missing score as unavailable", () => {
    expect(getSafetyDataState({
      score: null,
      calculatedAtUtc: null,
      newsItemCount: 0,
    }, "weekly", now)).toBe("unavailable");
  });

  it("marks weekly data older than eight days as stale", () => {
    expect(getSafetyDataState({
      score: 72,
      calculatedAtUtc: "2026-06-20T11:00:00Z",
      newsItemCount: 12,
    }, "weekly", now)).toBe("stale");
  });

  it("keeps recent weekly data available", () => {
    expect(getSafetyDataState({
      score: 72,
      calculatedAtUtc: "2026-06-29T11:00:00Z",
      newsItemCount: 12,
    }, "weekly", now)).toBe("available");
  });

  it("does not age out the long-term historical score", () => {
    expect(getSafetyDataState({
      score: 65,
      calculatedAtUtc: "2025-01-01T00:00:00Z",
      newsItemCount: 50,
    }, "historical", now)).toBe("available");
  });
});
