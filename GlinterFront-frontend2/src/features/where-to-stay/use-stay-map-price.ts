import { useEffect, useMemo, useState } from "react";
import type { MapOverlayArea } from "@/components/LeafletMap";
import { formatUsdPrice } from "@/shared/lib/price";
import { priceIndexApi } from "@/shared/services/api-price-index";
import { regionsApi } from "@/shared/services/api-regions";
import type { LoadState } from "@/shared/types/async-state";
import type {
  CountryDto,
  DistrictDto,
  GovernorateDto,
  NeighbourhoodDto,
  RegionHierarchyGids,
} from "@/shared/types/regions";
import type { PriceIndexFilters, PriceIndexResponse } from "@/shared/types/price-index";
import { getIndexColor } from "./map-modes";

type PriceOverlayRegion =
  | CountryDto
  | GovernorateDto
  | DistrictDto
  | NeighbourhoodDto;

type PriceOverlayLevel = "adm0" | "adm1" | "adm2" | "adm3";

interface PriceOverlayItem {
  region: PriceOverlayRegion;
  level: PriceOverlayLevel;
  index: PriceIndexResponse;
}

export function useStayMapPrice(
  region: RegionHierarchyGids,
  enabled: boolean,
) {
  const [state, setState] = useState<LoadState>({ status: "ready" });
  const [currentIndex, setCurrentIndex] = useState<PriceIndexResponse | null>(null);
  const [items, setItems] = useState<PriceOverlayItem[]>([]);

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
        priceIndexApi.get(region),
        getOverlayRegions(region),
      ]);

      const filters = overlayRegions.regions.map((overlayRegion) =>
        toPriceIndexFilters(region, overlayRegions.level, overlayRegion.gid),
      );
      const indexes = filters.length > 0
        ? await priceIndexApi.getBatch(filters)
        : [];
      const loadedItems = overlayRegions.regions.flatMap(
        (overlayRegion, index): PriceOverlayItem[] => {
          const indexResponse = indexes[index];
          return indexResponse
            ? [{
              region: overlayRegion,
              level: overlayRegions.level,
              index: indexResponse,
            }]
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
        message: error instanceof Error ? error.message : "Could not load price index heatmap.",
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
    const average = formatUsdPrice(combined.averagePrice, "No priced data");
    const suffix = indexValue == null
      ? "No priced data"
      : `${indexValue.toFixed(0)} price index - ${average} avg.`;

    return [{
      id: item.region.gid,
      name,
      value: indexValue,
      tooltip: `${name}: ${suffix}`,
      color: getIndexColor(indexValue),
      geometry: item.region.geometryGeoJson,
      fillOpacity: indexValue == null ? 0.18 : 0.5,
    }];
  }), [items]);

  const indexByRegion = useMemo(
    () => new Map(items.map((item) => [
      item.region.gid,
      item.index.combined.indexValue,
    ])),
    [items],
  );

  return {
    state,
    currentIndex,
    overlayAreas,
    indexByRegion,
  };
}

async function getOverlayRegions(region: RegionHierarchyGids): Promise<{
  level: PriceOverlayLevel;
  regions: PriceOverlayRegion[];
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

function toPriceIndexFilters(
  region: RegionHierarchyGids,
  level: PriceOverlayLevel,
  gid: number,
): PriceIndexFilters {
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

function getRegionName(region: PriceOverlayRegion): string {
  return region.nameEn || region.nameAr || region.pcode || `Region ${region.gid}`;
}
