import { describe, expect, it } from "vitest";
import { getSafeNotificationLink } from "./notification-links";

describe("notification links", () => {
  it("accepts supported application deep links", () => {
    expect(getSafeNotificationLink("/messages?thread=123e4567-e89b-42d3-a456-426614174000"))
      .toBe("/messages?thread=123e4567-e89b-42d3-a456-426614174000");
    expect(getSafeNotificationLink("/profile/me?tab=bookings"))
      .toBe("/profile/me?tab=bookings");
    expect(getSafeNotificationLink("/profile/me?tab=buddy-schedule"))
      .toBe("/profile/me?tab=buddy-schedule");
    expect(getSafeNotificationLink(
      "/profile/123e4567-e89b-42d3-a456-426614174000",
    )).toBe("/profile/123e4567-e89b-42d3-a456-426614174000");
  });

  it("rejects external, malformed, and unsupported destinations", () => {
    expect(getSafeNotificationLink("https://evil.example")).toBeUndefined();
    expect(getSafeNotificationLink("//evil.example")).toBeUndefined();
    expect(getSafeNotificationLink("/messages?thread=not-a-guid")).toBeUndefined();
    expect(getSafeNotificationLink("/admin")).toBeUndefined();
    expect(getSafeNotificationLink("/profile/me?tab=admin")).toBeUndefined();
  });
});
