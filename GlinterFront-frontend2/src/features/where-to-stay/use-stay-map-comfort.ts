import { useEffect, useMemo, useState } from "react";
import type { MapOverlayArea } from "@/components/LeafletMap";
import { comfortIndexApi } from "@/shared/services/api-comfort-index";
import { regionsApi } from "@/shared/services/api-regions";
import type { LoadState } from "@/shared/types/async-state";
import type { ComfortIndexFilters, ComfortIndexResponse } from "@/shared/types/comfort-index";
import type {
  CountryDto,
  DistrictDto,
  GovernorateDto,
  NeighbourhoodDto,
  RegionHierarchyGids,
} from "@/shared/types/regions";
import { getComfortIndexColor } from "./map-modes";

type ComfortOverlayRegion =
  | CountryDto
  | GovernorateDto
  | DistrictDto
  | NeighbourhoodDto;

type ComfortOverlayLevel = "adm0" | "adm1" | "adm2" | "adm3";

interface ComfortOverlayItem {
  region: ComfortOverlayRegion;
  index: ComfortIndexResponse;
}

export function useStayMapComfort(
  region: RegionHierarchyGids,
  enabled: boolean,
) {
  const [state, setState] = useState<LoadState>({ status: "ready" });
  const [currentIndex, setCurrentIndex] = useState<ComfortIndexResponse | null>(null);
  const [items, setItems] = useState<ComfortOverlayItem[]>([]);

  useEffect(() => {
    if (!enabled) {
      setState({ status: "ready" });
      setCurrentIndex(null);
      setItems([]);
      return;
    }

    let active = true;
    setState({ status: "loading" });

    async function load() {
      const [loadedCurrentIndex, overlayRegions] = await Promise.all([
        comfortIndexApi.get(region),
        getOverlayRegions(region),
      ]);
      const filters = overlayRegions.regions.map((overlayRegion) =>
        toComfortIndexFilters(region, overlayRegions.level, overlayRegion.gid),
      );
      const indexes = filters.length > 0
        ? await comfortIndexApi.getBatch(filters)
        : [];
      const loadedItems = overlayRegions.regions.flatMap(
        (overlayRegion, index): ComfortOverlayItem[] => {
          const indexResponse = indexes[index];
          return indexResponse
            ? [{ region: overlayRegion, index: indexResponse }]
            : [];
        },
      );

      if (!active) return;
      setCurrentIndex(loadedCurrentIndex);
      setItems(loadedItems);
      setState({ status: "ready" });
    }

    load().catch((error: unknown) => {
      if (!active) return;
      setCurrentIndex(null);
      setItems([]);
      setState({
        status: "error",
        message: error instanceof Error ? error.message : "Could not load comfort index heatmap.",
      });
    });

    return () => {
      active = false;
    };
  }, [
    enabled,
    region.adm0Gid,
    region.adm1Gid,
    region.adm2Gid,
    region.adm3Gid,
  ]);

  const overlayAreas = useMemo(() => items.flatMap((item): MapOverlayArea[] => {
    if (!item.region.geometryGeoJson) return [];

    const combined = item.index.combined;
    const indexValue = combined.indexValue;
    const name = getRegionName(item.region);
    const average = combined.averageScore == null
      ? "No rated data"
      : `${combined.averageScore.toFixed(2)} avg. score`;
    const suffix = indexValue == null
      ? "No rated data"
      : `${indexValue.toFixed(0)} comfort index - ${average}`;

    return [{
      id: item.region.gid,
      name,
      value: indexValue,
      tooltip: `${name}: ${suffix}`,
      color: getComfortIndexColor(indexValue),
      geometry: item.region.geometryGeoJson,
      fillOpacity: indexValue == null ? 0.18 : 0.5,
    }];
  }), [items]);

  return {
    state,
    currentIndex,
    overlayAreas,
  };
}

async function getOverlayRegions(region: RegionHierarchyGids): Promise<{
  level: ComfortOverlayLevel;
  regions: ComfortOverlayRegion[];
}> {
  const query = { geometryAccuracy: 8 };

  if (region.adm3Gid) {
    return {
      level: "adm3",
      regions: [await regionsApi.getNeighbourhood(region.adm3Gid, 8)],
    };
  }

  if (region.adm2Gid) {
    return {
      level: "adm3",
      regions: await regionsApi.getNeighbourhoods(region.adm2Gid, query),
    };
  }

  if (region.adm1Gid) {
    return {
      level: "adm2",
      regions: await regionsApi.getDistricts(region.adm1Gid, query),
    };
  }

  if (region.adm0Gid) {
    return {
      level: "adm1",
      regions: await regionsApi.getGovernorates(region.adm0Gid, query),
    };
  }

  return {
    level: "adm0",
    regions: await regionsApi.getCountries(query),
  };
}

function toComfortIndexFilters(
  region: RegionHierarchyGids,
  level: ComfortOverlayLevel,
  gid: number,
): ComfortIndexFilters {
  if (level === "adm0") return { adm0Gid: gid };
  if (level === "adm1") return { adm0Gid: region.adm0Gid, adm1Gid: gid };
  if (level === "adm2") {
    return {
      adm0Gid: region.adm0Gid,
      adm1Gid: region.adm1Gid,
      adm2Gid: gid,
    };
  }

  return {
    adm0Gid: region.adm0Gid,
    adm1Gid: region.adm1Gid,
    adm2Gid: region.adm2Gid,
    adm3Gid: gid,
  };
}

function getRegionName(region: ComfortOverlayRegion): string {
  return region.nameEn || region.nameAr || region.pcode || `Region ${region.gid}`;
}
