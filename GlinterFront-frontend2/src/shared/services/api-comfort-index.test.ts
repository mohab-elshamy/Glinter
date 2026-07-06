import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { comfortIndexApi } from "./api-comfort-index";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("comfortIndexApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("loads the overall comfort index", async () => {
    vi.mocked(request).mockResolvedValueOnce({});
    await comfortIndexApi.get();
    expect(request).toHaveBeenCalledWith("/comfort-index");
  });

  it("loads comfort indexes in one batch", async () => {
    vi.mocked(request).mockResolvedValueOnce([]);
    await comfortIndexApi.getBatch([{ adm1Gid: 2 }, { adm2Gid: 3 }]);
    expect(request).toHaveBeenCalledWith("/comfort-index/batch", {
      method: "POST",
      body: { filters: [{ adm1Gid: 2 }, { adm2Gid: 3 }] },
    });
  });

  it("loads a district comfort index", async () => {
    vi.mocked(request).mockResolvedValueOnce({});
    await comfortIndexApi.getByDistrict(3);
    expect(request).toHaveBeenCalledWith("/comfort-index/adm2/3");
  });
});
