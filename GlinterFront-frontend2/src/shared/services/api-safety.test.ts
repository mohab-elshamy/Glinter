import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { safetyApi } from "./api-safety";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("safetyApi", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("loads one district safety index", async () => {
    vi.mocked(request).mockResolvedValueOnce({ adm2Gid: 3 });
    await safetyApi.getByDistrict(3);
    expect(request).toHaveBeenCalledWith("/safety-index/adm2/3");
  });

  it("loads governorate safety indexes", async () => {
    vi.mocked(request).mockResolvedValueOnce([]);
    await safetyApi.getByGovernorate(2);
    expect(request).toHaveBeenCalledWith("/safety-index/adm1/2");
  });

  it("loads country safety indexes", async () => {
    vi.mocked(request).mockResolvedValueOnce([]);
    await safetyApi.getByCountry(1);
    expect(request).toHaveBeenCalledWith("/safety-index/adm0/1");
  });
});
