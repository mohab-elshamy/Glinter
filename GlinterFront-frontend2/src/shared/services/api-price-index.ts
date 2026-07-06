import { request } from "@/shared/lib/api-client";
import type {
  PriceIndexFilters,
  PriceIndexResponse,
} from "@/shared/types/price-index";

function toQuery(filters: PriceIndexFilters): string {
  const params = new URLSearchParams();

  Object.entries(filters).forEach(([key, value]) => {
    if (value != null) params.set(key, String(value));
  });

  const query = params.toString();
  return query ? `?${query}` : "";
}

export const priceIndexApi = {
  get: (filters: PriceIndexFilters = {}) =>
    request<PriceIndexResponse>(`/price-index${toQuery(filters)}`),

  getBatch: (filters: PriceIndexFilters[]) =>
    request<PriceIndexResponse[]>("/price-index/batch", {
      method: "POST",
      body: { filters },
    }),

  getByCountry: (adm0Gid: number) =>
    request<PriceIndexResponse>(`/price-index/adm0/${adm0Gid}`),

  getByGovernorate: (adm1Gid: number) =>
    request<PriceIndexResponse>(`/price-index/adm1/${adm1Gid}`),

  getByDistrict: (adm2Gid: number) =>
    request<PriceIndexResponse>(`/price-index/adm2/${adm2Gid}`),

  getByNeighbourhood: (adm3Gid: number) =>
    request<PriceIndexResponse>(`/price-index/adm3/${adm3Gid}`),
};
