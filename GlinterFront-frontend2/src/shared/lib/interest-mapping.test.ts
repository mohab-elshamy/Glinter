import { describe, expect, it } from "vitest";
import { mapInterestsToCategories, mapInterestToCategory } from "./interest-mapping";

describe("profile interest mapping", () => {
  it("maps English aliases case-insensitively", () => {
    expect(mapInterestToCategory("Museums")).toBe("Historical");
    expect(mapInterestToCategory("RESTAURANTS")).toBe("Dining");
  });

  it("maps Arabic aliases and removes duplicates", () => {
    expect(mapInterestsToCategories(["متاحف", "History", "شواطئ"])).toEqual(["Historical", "Nature"]);
  });

  it("does not invent a category", () => {
    expect(mapInterestToCategory("Unmapped")).toBeUndefined();
  });
});
