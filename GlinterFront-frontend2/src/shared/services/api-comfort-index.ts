import { request } from "@/shared/lib/api-client";
import type {
  ComfortIndexFilters,
  ComfortIndexResponse,
} from "@/shared/types/comfort-index";

function toQuery(filters: ComfortIndexFilters): string {
  const params = new URLSearchParams();

  Object.entries(filters).forEach(([key, value]) => {
    if (value != null) params.set(key, String(value));
  });

  const query = params.toString();
  return query ? `?${query}` : "";
}

export const comfortIndexApi = {
  get: (filters: ComfortIndexFilters = {}) =>
    request<ComfortIndexResponse>(`/comfort-index${toQuery(filters)}`),

  getBatch: (filters: ComfortIndexFilters[]) =>
    request<ComfortIndexResponse[]>("/comfort-index/batch", {
      method: "POST",
      body: { filters },
    }),

  getByCountry: (adm0Gid: number) =>
    request<ComfortIndexResponse>(`/comfort-index/adm0/${adm0Gid}`),

  getByGovernorate: (adm1Gid: number) =>
    request<ComfortIndexResponse>(`/comfort-index/adm1/${adm1Gid}`),

  getByDistrict: (adm2Gid: number) =>
    request<ComfortIndexResponse>(`/comfort-index/adm2/${adm2Gid}`),

  getByNeighbourhood: (adm3Gid: number) =>
    request<ComfortIndexResponse>(`/comfort-index/adm3/${adm3Gid}`),
};
