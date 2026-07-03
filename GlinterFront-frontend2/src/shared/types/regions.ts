export interface RegionListQuery {
  search?: string;
  geometryAccuracy?: number;
  page?: number;
  pageSize?: number;
}

interface RegionBase {
  gid: number;
  nameEn?: string;
  nameAr?: string;
  pcode: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt: string;
  geometryGeoJson?: Geometry | Feature | null;
}

export interface CountryDto extends RegionBase {
  nameEn: string;
  flagUrl?: string;
}

export interface GovernorateDto extends RegionBase {
  adm0Gid: number;
  nameEn: string;
}

export interface DistrictDto extends RegionBase {
  adm1Gid: number;
  nameEn: string;
}

export interface NeighbourhoodDto extends RegionBase {
  adm2Gid: number;
}

export interface RegionHierarchyGids {
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
}

export interface RegionSelection extends RegionHierarchyGids {
  country?: CountryDto;
  governorate?: GovernorateDto;
  district?: DistrictDto;
  neighbourhood?: NeighbourhoodDto;
}

export interface CreateCountryRequest {
  nameEn: string;
  nameAr?: string;
  pcode: string;
  imageUrl?: string;
  flagUrl?: string;
}

export type UpdateCountryRequest = Omit<CreateCountryRequest, "pcode">;

export interface CreateGovernorateRequest {
  adm0Gid: number;
  nameEn: string;
  nameAr?: string;
  pcode: string;
  imageUrl?: string;
}

export interface UpdateRegionRequest {
  nameEn: string;
  nameAr?: string;
  imageUrl?: string;
}

export interface CreateDistrictRequest extends Omit<CreateGovernorateRequest, "adm0Gid"> {
  adm1Gid: number;
}

export interface CreateNeighbourhoodRequest extends Omit<CreateGovernorateRequest, "adm0Gid"> {
  adm2Gid: number;
}

export interface GeoJsonImportResult {
  success: boolean;
  layer: string;
  totalFeatures: number;
  inserted: number;
  updated: number;
  skipped: number;
  errors: string[];
}

export type RegionLevel = "adm0" | "adm1" | "adm2" | "adm3";
import type { Feature, Geometry } from "geojson";
