// @vitest-environment jsdom

import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { regionsApi } from "./api-regions";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("regionsApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("loads a complete cascading hierarchy", async () => {
    vi.mocked(request)
      .mockResolvedValueOnce([{ gid: 1, nameEn: "Egypt" }])
      .mockResolvedValueOnce([{ gid: 2, nameEn: "Cairo", adm0Gid: 1 }])
      .mockResolvedValueOnce([{ gid: 3, nameEn: "Central", adm1Gid: 2 }])
      .mockResolvedValueOnce([{ gid: 4, nameEn: "Downtown", adm2Gid: 3 }]);

    await regionsApi.getCountries();
    await regionsApi.getGovernorates(1);
    await regionsApi.getDistricts(2);
    await regionsApi.getNeighbourhoods(3);

    expect(request).toHaveBeenNthCalledWith(1, "/regions/countries?page=1&pageSize=100");
    expect(request).toHaveBeenNthCalledWith(2, "/regions/countries/1/governorates?page=1&pageSize=100");
    expect(request).toHaveBeenNthCalledWith(3, "/regions/governorates/2/districts?page=1&pageSize=100");
    expect(request).toHaveBeenNthCalledWith(4, "/regions/districts/3/neighbourhoods?page=1&pageSize=100");
  });

  it("resolves hierarchy IDs from a map point", async () => {
    vi.mocked(request).mockResolvedValueOnce({
      adm0Gid: 1,
      adm1Gid: 2,
      adm2Gid: 3,
      adm3Gid: 4,
    });

    await regionsApi.getByPoint(30.0444, 31.2357);

    expect(request).toHaveBeenCalledWith(
      "/regions/by-point?lat=30.0444&lon=31.2357",
    );
  });

  it("resolves readable region names without changing a filter selection", async () => {
    vi.mocked(request)
      .mockResolvedValueOnce({ adm0Gid: 1, adm1Gid: 2, adm2Gid: 3, adm3Gid: 4 })
      .mockResolvedValueOnce({ gid: 1, nameEn: "Egypt" })
      .mockResolvedValueOnce({ gid: 2, nameEn: "Cairo" })
      .mockResolvedValueOnce({ gid: 3, nameEn: "Maadi" })
      .mockResolvedValueOnce({ gid: 4, nameEn: "Degla" });

    const selection = await regionsApi.getSelectionByPoint(29.96, 31.25);

    expect(selection.country?.nameEn).toBe("Egypt");
    expect(selection.governorate?.nameEn).toBe("Cairo");
    expect(selection.district?.nameEn).toBe("Maadi");
    expect(selection.neighbourhood?.nameEn).toBe("Degla");
    expect(request).toHaveBeenNthCalledWith(1, "/regions/by-point?lat=29.96&lon=31.25");
    expect(request).toHaveBeenNthCalledWith(2, "/regions/countries/1?geometryAccuracy=0");
    expect(request).toHaveBeenNthCalledWith(3, "/regions/governorates/2?geometryAccuracy=0");
    expect(request).toHaveBeenNthCalledWith(4, "/regions/districts/3?geometryAccuracy=0");
    expect(request).toHaveBeenNthCalledWith(5, "/regions/neighbourhoods/4?geometryAccuracy=0");
  });

  it("uploads GeoJSON as multipart form data", async () => {
    vi.mocked(request).mockResolvedValueOnce({ success: true });
    const file = new File(["{}"], "districts.geojson", {
      type: "application/geo+json",
    });

    await regionsApi.importGeoJson("adm2", file);

    const options = vi.mocked(request).mock.calls[0][1] as {
      method: string;
      body: FormData;
    };
    expect(request).toHaveBeenCalledWith(
      "/admin/regions/import/adm2",
      expect.objectContaining({ method: "POST" }),
    );
    expect(options.body).toBeInstanceOf(FormData);
    expect(options.body.get("file")).toBe(file);
  });
});
