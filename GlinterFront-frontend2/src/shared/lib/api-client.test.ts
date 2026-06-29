// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiActivity } from "./api-activity";
import { ApiError, request } from "./api-client";
import { authStorage } from "./auth";

const problemResponse = (
  status: number,
  body: Record<string, unknown>,
): Response => new Response(JSON.stringify(body), {
  status,
  headers: { "Content-Type": "application/problem+json" },
});

describe("API client", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    localStorage.clear();
  });

  it("parses backend Problem Details", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(problemResponse(403, {
      title: "Forbidden.",
      detail: "Only administrators can perform this action.",
      errorCode: "forbidden",
      traceId: "trace-123",
    }));

    await expect(request("/admin/users")).rejects.toMatchObject({
      name: "ApiError",
      status: 403,
      message: "Only administrators can perform this action.",
      errorCode: "forbidden",
      traceId: "trace-123",
    });
  });

  it("reports network failures consistently and clears loading state", async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new TypeError("Failed to fetch"));

    await expect(request("/health")).rejects.toEqual(
      expect.objectContaining<ApiError>({
        status: 0,
        errorCode: "network_error",
      }),
    );
    expect(apiActivity.getSnapshot()).toBe(0);
  });

  it("rotates the refresh token and retries a failed authorized request", async () => {
    authStorage.setTokens(
      "expired-access-token",
      "old-refresh-token",
      "2026-07-29T00:00:00Z",
    );

    vi.mocked(fetch)
      .mockResolvedValueOnce(problemResponse(401, {
        title: "Unauthorized.",
        detail: "The access token has expired.",
        errorCode: "authentication_required",
      }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        userId: "user-1",
        fullName: "Traveler",
        email: "traveler@example.com",
        roles: ["Traveler"],
        token: "new-access-token",
        refreshToken: "new-refresh-token",
        refreshTokenExpiresAtUtc: "2026-08-29T00:00:00Z",
      }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ value: "ok" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }));

    await expect(request<{ value: string }>("/profiles/me"))
      .resolves.toEqual({ value: "ok" });

    expect(fetch).toHaveBeenCalledTimes(3);
    expect(authStorage.getToken()).toBe("new-access-token");
    expect(authStorage.getRefreshToken()).toBe("new-refresh-token");

    const retryOptions = vi.mocked(fetch).mock.calls[2][1] as RequestInit;
    expect(retryOptions.headers).toMatchObject({
      Authorization: "Bearer new-access-token",
    });
    expect(apiActivity.getSnapshot()).toBe(0);
  });

  it("shares one token rotation across concurrent unauthorized requests", async () => {
    authStorage.setTokens(
      "expired-access-token",
      "old-refresh-token",
      "2026-07-29T00:00:00Z",
    );
    let refreshCalls = 0;

    vi.mocked(fetch).mockImplementation(async (input, init) => {
      const url = String(input);
      const headers = init?.headers as Record<string, string> | undefined;

      if (url.endsWith("/auth/refresh")) {
        refreshCalls += 1;
        return new Response(JSON.stringify({
          userId: "user-1",
          fullName: "Traveler",
          email: "traveler@example.com",
          roles: ["Traveler"],
          token: "new-access-token",
          refreshToken: "new-refresh-token",
          refreshTokenExpiresAtUtc: "2026-08-29T00:00:00Z",
        }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }

      if (headers?.Authorization === "Bearer expired-access-token") {
        return problemResponse(401, {
          detail: "The access token has expired.",
          errorCode: "authentication_required",
        });
      }

      return new Response(JSON.stringify({ value: "ok" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    });

    await expect(Promise.all([
      request<{ value: string }>("/profiles/me"),
      request<{ value: string }>("/experience-bookings/my"),
    ])).resolves.toEqual([{ value: "ok" }, { value: "ok" }]);

    expect(refreshCalls).toBe(1);
    expect(fetch).toHaveBeenCalledTimes(5);
    expect(apiActivity.getSnapshot()).toBe(0);
  });
});
