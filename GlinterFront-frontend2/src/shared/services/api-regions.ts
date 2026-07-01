import { request } from "@/shared/lib/api-client";
import type {
  CountryDto,
  CreateCountryRequest,
  CreateDistrictRequest,
  CreateGovernorateRequest,
  CreateNeighbourhoodRequest,
  DistrictDto,
  GeoJsonImportResult,
  GovernorateDto,
  NeighbourhoodDto,
  RegionHierarchyGids,
  RegionSelection,
  RegionLevel,
  RegionListQuery,
  UpdateCountryRequest,
  UpdateRegionRequest,
} from "@/shared/types/regions";

const queryString = (params?: RegionListQuery) => {
  if (!params) return "";
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== "") query.set(key, String(value));
  });
  const value = query.toString();
  return value ? `?${value}` : "";
};

const listAll = async <T>(path: string, params?: RegionListQuery): Promise<T[]> => {
  const pageSize = 100;
  const items: T[] = [];
  for (let page = 1; page <= 100; page += 1) {
    const batch = await request<T[]>(`${path}${queryString({ ...params, page, pageSize })}`);
    items.push(...batch);
    if (batch.length < pageSize) break;
  }
  return items;
};

export const regionsApi = {
  getCountries: (params?: RegionListQuery) =>
    listAll<CountryDto>("/regions/countries", params),

  getCountry: (gid: number, geometryAccuracy = 0) =>
    request<CountryDto>(`/regions/countries/${gid}?geometryAccuracy=${geometryAccuracy}`),

  getGovernorates: (adm0Gid?: number, params?: RegionListQuery) =>
    listAll<GovernorateDto>(
      adm0Gid ? `/regions/countries/${adm0Gid}/governorates` : "/regions/governorates",
      params,
    ),

  getGovernorate: (gid: number, geometryAccuracy = 0) =>
    request<GovernorateDto>(`/regions/governorates/${gid}?geometryAccuracy=${geometryAccuracy}`),

  getDistricts: (adm1Gid?: number, params?: RegionListQuery) =>
    listAll<DistrictDto>(
      adm1Gid ? `/regions/governorates/${adm1Gid}/districts` : "/regions/districts",
      params,
    ),

  getDistrict: (gid: number, geometryAccuracy = 0) =>
    request<DistrictDto>(`/regions/districts/${gid}?geometryAccuracy=${geometryAccuracy}`),

  getNeighbourhoods: (adm2Gid?: number, params?: RegionListQuery) =>
    listAll<NeighbourhoodDto>(
      adm2Gid ? `/regions/districts/${adm2Gid}/neighbourhoods` : "/regions/neighbourhoods",
      params,
    ),

  getNeighbourhood: (gid: number, geometryAccuracy = 0) =>
    request<NeighbourhoodDto>(`/regions/neighbourhoods/${gid}?geometryAccuracy=${geometryAccuracy}`),

  getByPoint: (lat: number, lon: number) =>
    request<RegionHierarchyGids>(`/regions/by-point?lat=${lat}&lon=${lon}`),

  getSelectionByPoint: async (lat: number, lon: number): Promise<RegionSelection> => {
    const gids = await request<RegionHierarchyGids>(`/regions/by-point?lat=${lat}&lon=${lon}`);
    const [country, governorate, district, neighbourhood] = await Promise.all([
      gids.adm0Gid
        ? request<CountryDto>(`/regions/countries/${gids.adm0Gid}?geometryAccuracy=0`)
        : undefined,
      gids.adm1Gid
        ? request<GovernorateDto>(`/regions/governorates/${gids.adm1Gid}?geometryAccuracy=0`)
        : undefined,
      gids.adm2Gid
        ? request<DistrictDto>(`/regions/districts/${gids.adm2Gid}?geometryAccuracy=0`)
        : undefined,
      gids.adm3Gid
        ? request<NeighbourhoodDto>(`/regions/neighbourhoods/${gids.adm3Gid}?geometryAccuracy=0`)
        : undefined,
    ]);
    return { ...gids, country, governorate, district, neighbourhood };
  },

  createCountry: (data: CreateCountryRequest) =>
    request<CountryDto>("/regions/countries", { method: "POST", body: data }),

  updateCountry: (gid: number, data: UpdateCountryRequest) =>
    request<CountryDto>(`/regions/countries/${gid}`, { method: "PUT", body: data }),

  deleteCountry: (gid: number) =>
    request<void>(`/regions/countries/${gid}`, { method: "DELETE" }),

  createGovernorate: (data: CreateGovernorateRequest) =>
    request<GovernorateDto>("/regions/governorates", { method: "POST", body: data }),

  updateGovernorate: (gid: number, data: UpdateRegionRequest) =>
    request<GovernorateDto>(`/regions/governorates/${gid}`, { method: "PUT", body: data }),

  deleteGovernorate: (gid: number) =>
    request<void>(`/regions/governorates/${gid}`, { method: "DELETE" }),

  createDistrict: (data: CreateDistrictRequest) =>
    request<DistrictDto>("/regions/districts", { method: "POST", body: data }),

  updateDistrict: (gid: number, data: UpdateRegionRequest) =>
    request<DistrictDto>(`/regions/districts/${gid}`, { method: "PUT", body: data }),

  deleteDistrict: (gid: number) =>
    request<void>(`/regions/districts/${gid}`, { method: "DELETE" }),

  createNeighbourhood: (data: CreateNeighbourhoodRequest) =>
    request<NeighbourhoodDto>("/regions/neighbourhoods", { method: "POST", body: data }),

  updateNeighbourhood: (gid: number, data: UpdateRegionRequest) =>
    request<NeighbourhoodDto>(`/regions/neighbourhoods/${gid}`, { method: "PUT", body: data }),

  deleteNeighbourhood: (gid: number) =>
    request<void>(`/regions/neighbourhoods/${gid}`, { method: "DELETE" }),

  importGeoJson: (level: RegionLevel, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<GeoJsonImportResult>(`/admin/regions/import/${level}`, {
      method: "POST",
      body: form,
    });
  },

  importAllLocal: () =>
    request<GeoJsonImportResult[]>("/admin/regions/import/all-local", { method: "POST" }),
};
