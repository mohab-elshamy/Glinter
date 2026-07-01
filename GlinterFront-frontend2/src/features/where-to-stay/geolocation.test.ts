import { describe, expect, it } from "vitest";
import {
  geolocationFailureFromCode,
  geolocationFailureMessage,
} from "./geolocation";

describe("current-location failures", () => {
  it("maps browser permission, timeout, and unavailable failures", () => {
    expect(geolocationFailureFromCode(1)).toBe("denied");
    expect(geolocationFailureFromCode(2)).toBe("unavailable");
    expect(geolocationFailureFromCode(3)).toBe("timeout");
  });

  it("provides actionable messages for every supported failure", () => {
    expect(geolocationFailureMessage.denied).toContain("browser settings");
    expect(geolocationFailureMessage.unavailable).toContain("location services");
    expect(geolocationFailureMessage.timeout).toContain("retry");
    expect(geolocationFailureMessage.insecure).toContain("HTTPS");
    expect(geolocationFailureMessage.unsupported).toContain("does not support");
  });
});
