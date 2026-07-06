// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { regionsApi } from "@/shared/services/api-regions";
import { detectedRegionLabel, useCurrentLocation } from "./use-current-location";

vi.mock("@/shared/services/api-regions", () => ({
  regionsApi: {
    getSelectionByPoint: vi.fn(),
  },
}));

describe("useCurrentLocation", () => {
  beforeEach(() => {
    vi.mocked(regionsApi.getSelectionByPoint).mockReset();
    Object.defineProperty(window, "isSecureContext", {
      configurable: true,
      value: true,
    });
  });

  it("stores browser coordinates and identifies their backend region", async () => {
    vi.mocked(regionsApi.getSelectionByPoint).mockResolvedValue({
      adm0Gid: 1,
      adm1Gid: 2,
      adm2Gid: 3,
      country: { gid: 1, nameEn: "Egypt", pcode: "EG", createdAt: "", updatedAt: "" },
      governorate: { gid: 2, adm0Gid: 1, nameEn: "Cairo", pcode: "CA", createdAt: "", updatedAt: "" },
      district: { gid: 3, adm1Gid: 2, nameEn: "Maadi", pcode: "MA", createdAt: "", updatedAt: "" },
    });
    Object.defineProperty(navigator, "geolocation", {
      configurable: true,
      value: {
        getCurrentPosition: (
          success: PositionCallback,
        ) => success({
          coords: {
            latitude: 29.96,
            longitude: 31.25,
            accuracy: 18,
            altitude: null,
            altitudeAccuracy: null,
            heading: null,
            speed: null,
            toJSON: () => ({}),
          },
          timestamp: Date.now(),
          toJSON: () => ({}),
        }),
      },
    });

    const { result } = renderHook(() => useCurrentLocation());
    act(() => result.current.locate());

    await waitFor(() => expect(result.current.state.detectedRegion).toBeDefined());
    expect(result.current.state.status).toBe("ready");
    expect(result.current.state.position).toEqual({
      latitude: 29.96,
      longitude: 31.25,
      accuracy: 18,
    });
    expect(regionsApi.getSelectionByPoint).toHaveBeenCalledWith(29.96, 31.25);
    expect(detectedRegionLabel(result.current.state.detectedRegion)).toBe("Egypt › Cairo › Maadi");
  });

  it("reports an insecure deployment before requesting device location", () => {
    Object.defineProperty(window, "isSecureContext", {
      configurable: true,
      value: false,
    });
    const getCurrentPosition = vi.fn();
    Object.defineProperty(navigator, "geolocation", {
      configurable: true,
      value: { getCurrentPosition },
    });

    const { result } = renderHook(() => useCurrentLocation());
    act(() => result.current.locate());

    expect(result.current.state.status).toBe("insecure");
    expect(result.current.state.regionMessage).toContain("HTTPS");
    expect(getCurrentPosition).not.toHaveBeenCalled();
  });
});
