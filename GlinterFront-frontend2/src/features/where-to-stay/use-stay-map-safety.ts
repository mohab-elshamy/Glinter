import { useEffect, useMemo, useState } from "react";
import { regionsApi } from "@/shared/services/api-regions";
import { safetyApi } from "@/shared/services/api-safety";
import type { LoadState } from "@/shared/types/async-state";
import type { DistrictDto, RegionHierarchyGids } from "@/shared/types/regions";
import type { DistrictSafetyIndex, SafetyPeriod } from "@/shared/types/safety";
import { getSafetyColor, getSafetyDataState, getScoreForPeriod } from "@/shared/lib/safety";
import type { MapOverlayArea } from "@/components/LeafletMap";

export function useStayMapSafety(
  region: RegionHierarchyGids,
  enabled: boolean,
  period: SafetyPeriod,
) {
  const [scores, setScores] = useState<DistrictSafetyIndex[]>([]);
  const [districts, setDistricts] = useState<DistrictDto[]>([]);
  const [state, setState] = useState<LoadState>({ status: "ready" });

  useEffect(() => {
    if (!enabled || !region.adm0Gid) {
      setScores([]);
      setDistricts([]);
      setState({ status: "ready" });
      return;
    }

    let active = true;
    setState({ status: "loading" });

    const scoreRequest = region.adm2Gid
      ? safetyApi.getByDistrict(region.adm2Gid).then((item) => [item])
      : region.adm1Gid
        ? safetyApi.getByGovernorate(region.adm1Gid)
        : safetyApi.getByCountry(region.adm0Gid);

    const geometryRequest = region.adm2Gid
      ? regionsApi.getDistrict(region.adm2Gid, 8).then((item) => [item])
      : region.adm1Gid
        ? regionsApi.getDistricts(region.adm1Gid, { geometryAccuracy: 8 })
        : Promise.resolve([]);

    Promise.all([scoreRequest, geometryRequest])
      .then(([loadedScores, loadedDistricts]) => {
        if (!active) return;
        setScores(loadedScores);
        setDistricts(loadedDistricts);
        setState({ status: "ready" });
      })
      .catch((error: unknown) => {
        if (!active) return;
        setScores([]);
        setDistricts([]);
        setState({
          status: "error",
          message: error instanceof Error ? error.message : "Could not load safety overlays.",
        });
      });

    return () => {
      active = false;
    };
  }, [enabled, region.adm0Gid, region.adm1Gid, region.adm2Gid]);

  const scoreByDistrict = useMemo(
    () => new Map(scores.map((item) => [
      item.adm2Gid,
      getScoreForPeriod(item, period).score,
    ])),
    [period, scores],
  );

  const districtById = useMemo(
    () => new Map(districts.map((item) => [item.gid, item])),
    [districts],
  );

  const overlayAreas = useMemo(() => scores.flatMap((item): MapOverlayArea[] => {
    const district = districtById.get(item.adm2Gid);
    if (!district?.geometryGeoJson) return [];
    const score = getScoreForPeriod(item, period);
    const dataState = getSafetyDataState(score, period);
    return [{
      id: item.adm2Gid,
      name: item.nameEn,
      value: score.score,
      color: getSafetyColor(score.score),
      geometry: district.geometryGeoJson,
      fillOpacity: dataState === "unavailable" ? 0.18 : 0.5,
      dashArray: dataState === "stale" ? "7 5" : undefined,
    }];
  }), [districtById, period, scores]);

  return { state, scoreByDistrict, overlayAreas };
}
