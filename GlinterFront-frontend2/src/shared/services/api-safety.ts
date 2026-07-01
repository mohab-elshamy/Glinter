import { request } from "@/shared/lib/api-client";
import type { DistrictSafetyIndex } from "@/shared/types/safety";

export const safetyApi = {
  getByDistrict: (adm2Gid: number) =>
    request<DistrictSafetyIndex>(`/safety-index/adm2/${adm2Gid}`),

  getByGovernorate: (adm1Gid: number) =>
    request<DistrictSafetyIndex[]>(`/safety-index/adm1/${adm1Gid}`),

  getByCountry: (adm0Gid: number) =>
    request<DistrictSafetyIndex[]>(`/safety-index/adm0/${adm0Gid}`),
};
