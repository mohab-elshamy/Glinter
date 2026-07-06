import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { priceIndexApi } from "./api-price-index";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("priceIndexApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("loads the overall price index", async () => {
    vi.mocked(request).mockResolvedValueOnce({});
    await priceIndexApi.get();
    expect(request).toHaveBeenCalledWith("/price-index");
  });

  it("loads a filtered price index", async () => {
    vi.mocked(request).mockResolvedValueOnce({});
    await priceIndexApi.get({ adm1Gid: 2, adm2Gid: 3 });
    expect(request).toHaveBeenCalledWith("/price-index?adm1Gid=2&adm2Gid=3");
  });

  it("loads price indexes in one batch", async () => {
    vi.mocked(request).mockResolvedValueOnce([]);
    await priceIndexApi.getBatch([{ adm1Gid: 2 }, { adm2Gid: 3 }]);
    expect(request).toHaveBeenCalledWith("/price-index/batch", {
      method: "POST",
      body: { filters: [{ adm1Gid: 2 }, { adm2Gid: 3 }] },
    });
  });

  it("loads a district price index", async () => {
    vi.mocked(request).mockResolvedValueOnce({});
    await priceIndexApi.getByDistrict(3);
    expect(request).toHaveBeenCalledWith("/price-index/adm2/3");
  });
});
