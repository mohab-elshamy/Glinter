// @vitest-environment jsdom

import { beforeEach, describe, expect, it } from "vitest";
import { authStorage } from "./auth";

describe("authentication storage", () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it("keeps tokens in session storage rather than persistent local storage", () => {
    authStorage.setTokens("access", "refresh", "2026-08-01T00:00:00Z");

    expect(sessionStorage.getItem("token")).toBe("access");
    expect(sessionStorage.getItem("refreshToken")).toBe("refresh");
    expect(localStorage.getItem("token")).toBeNull();
    expect(localStorage.getItem("refreshToken")).toBeNull();
  });

  it("migrates and removes legacy persistent tokens on first read", () => {
    localStorage.setItem("token", "legacy-access");

    expect(authStorage.getToken()).toBe("legacy-access");
    expect(sessionStorage.getItem("token")).toBe("legacy-access");
    expect(localStorage.getItem("token")).toBeNull();
  });
});
