import { useEffect, useMemo, useState } from "react";
import { AlertTriangle, Clock3, RefreshCw, ShieldCheck } from "lucide-react";
import Footer from "@/components/Footer";
import Navbar from "@/components/Navbar";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import SafetyMap, { type SafetyMapArea } from "./SafetyMap";
import { safetyApi } from "@/shared/services/api-safety";
import { regionsApi } from "@/shared/services/api-regions";
import {
  getSafetyColor,
  getSafetyDataState,
  getScoreForPeriod,
} from "@/shared/lib/safety";
import type { LoadState } from "@/shared/types/async-state";
import type { DistrictDto, RegionHierarchyGids } from "@/shared/types/regions";
import type {
  DistrictSafetyIndex,
  SafetyPeriod,
  SafetyScore,
} from "@/shared/types/safety";

const dateTime = (value?: string | null) => {
  if (!value) return "Not calculated";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown date" : date.toLocaleString();
};

const SafetyScorePanel = ({
  title,
  score,
  period,
}: {
  title: string;
  score: SafetyScore;
  period: SafetyPeriod;
}) => {
  const state = getSafetyDataState(score, period);
  const stateLabel = state === "available"
    ? "Available"
    : state === "stale"
      ? "Stale"
      : "Unavailable";

  return (
    <article className="rounded-xl border border-border/50 bg-secondary/35 p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="font-semibold">{title}</h3>
          <p className="mt-1 text-xs text-muted-foreground">
            {period === "weekly" ? "Recent news window" : "Long-term news history"}
          </p>
        </div>
        <span className={`rounded-full px-2 py-1 text-[10px] font-semibold ${
          state === "available"
            ? "bg-green-500/15 text-green-300"
            : state === "stale"
              ? "bg-amber-500/15 text-amber-300"
              : "bg-slate-500/15 text-slate-300"
        }`}>
          {stateLabel}
        </span>
      </div>

      <div className="my-4 flex items-end gap-2">
        <strong
          className="text-4xl"
          style={{ color: getSafetyColor(score.score) }}
        >
          {score.score ?? "—"}
        </strong>
        <span className="pb-1 text-xs text-muted-foreground">/ 100</span>
      </div>

      <p className="min-h-10 text-sm text-foreground/90">
        {score.generalSafetyDescription || "No safety description is available yet."}
      </p>
      {score.trendingEventDescription && (
        <p className="mt-3 rounded-lg border border-amber-500/20 bg-amber-500/10 p-3 text-xs text-amber-100">
          {score.trendingEventDescription}
        </p>
      )}
      <div className="mt-4 flex flex-wrap gap-x-4 gap-y-1 text-[11px] text-muted-foreground">
        <span className="flex items-center gap-1"><Clock3 className="h-3 w-3" />{dateTime(score.calculatedAtUtc)}</span>
        <span>{score.newsItemCount} news items</span>
      </div>
    </article>
  );
};

const WhereToGo = () => {
  const [region, setRegion] = useState<RegionHierarchyGids>({});
  const [period, setPeriod] = useState<SafetyPeriod>("weekly");
  const [areas, setAreas] = useState<DistrictSafetyIndex[]>([]);
  const [districts, setDistricts] = useState<DistrictDto[]>([]);
  const [selectedAdm2Gid, setSelectedAdm2Gid] = useState<number>();
  const [loadState, setLoadState] = useState<LoadState>({ status: "ready" });
  const [geometryError, setGeometryError] = useState("");
  const [retryVersion, setRetryVersion] = useState(0);

  useEffect(() => {
    if (!region.adm0Gid) {
      setAreas([]);
      setDistricts([]);
      setSelectedAdm2Gid(undefined);
      setLoadState({ status: "ready" });
      return;
    }

    let active = true;
    setLoadState({ status: "loading" });
    setGeometryError("");

    const safetyRequest = region.adm2Gid
      ? safetyApi.getByDistrict(region.adm2Gid).then((item) => [item])
      : region.adm1Gid
        ? safetyApi.getByGovernorate(region.adm1Gid)
        : safetyApi.getByCountry(region.adm0Gid);

    const geometryRequest = region.adm2Gid
      ? regionsApi.getDistrict(region.adm2Gid, 8).then((item) => [item])
      : region.adm1Gid
        ? regionsApi.getDistricts(region.adm1Gid, { geometryAccuracy: 8 })
        : Promise.resolve([]);

    safetyRequest
      .then((items) => {
        if (!active) return;
        setAreas(items);
        setSelectedAdm2Gid((current) =>
          items.some((item) => item.adm2Gid === current)
            ? current
            : (items.find((item) => getScoreForPeriod(item, period).score != null) ?? items[0])?.adm2Gid,
        );
        setLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        if (!active) return;
        setAreas([]);
        setLoadState({
          status: "error",
          message: error instanceof Error ? error.message : "Could not load safety indexes.",
        });
      });

    geometryRequest
      .then((items) => {
        if (active) setDistricts(items);
      })
      .catch((error: unknown) => {
        if (!active) return;
        setDistricts([]);
        setGeometryError(error instanceof Error ? error.message : "Could not load district boundaries.");
      });

    return () => {
      active = false;
    };
  }, [period, region.adm0Gid, region.adm1Gid, region.adm2Gid, retryVersion]);

  const selectedArea = areas.find((item) => item.adm2Gid === selectedAdm2Gid);
  const districtById = useMemo(
    () => new Map(districts.map((item) => [item.gid, item])),
    [districts],
  );

  const mapAreas = useMemo(() => areas.flatMap((area): SafetyMapArea[] => {
    const district = districtById.get(area.adm2Gid);
    if (!district?.geometryGeoJson) return [];
    const score = getScoreForPeriod(area, period);
    return [{
      adm2Gid: area.adm2Gid,
      name: area.nameEn,
      score: score.score,
      state: getSafetyDataState(score, period),
      geometry: district.geometryGeoJson,
    }];
  }), [areas, districtById, period]);

  const summary = useMemo(() => areas.reduce(
    (counts, area) => {
      const state = getSafetyDataState(getScoreForPeriod(area, period), period);
      counts[state] += 1;
      return counts;
    },
    { available: 0, stale: 0, unavailable: 0 },
  ), [areas, period]);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-7xl px-4 py-10">
        <header className="mb-7">
          <div className="mb-3 flex items-center gap-2 text-primary">
            <ShieldCheck className="h-6 w-6" />
            <span className="text-sm font-semibold uppercase tracking-[0.2em]">Live backend data</span>
          </div>
          <h1 className="text-3xl font-extrabold sm:text-4xl">
            Egypt Safety <span className="text-gradient-orange">Index</span>
          </h1>
          <p className="mt-3 max-w-3xl text-sm text-muted-foreground">
            Compare recent weekly signals with long-term historical context. Scores are AI-assisted summaries of news data, not guarantees or emergency advice.
          </p>
        </header>

        <section className="card-glass mb-6 p-5">
          <RegionCascadeSelect
            value={region}
            onChange={setRegion}
            includeNeighbourhood={false}
            label="Choose a country, governorate, or district"
          />
          <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
            <div className="flex rounded-lg border border-border bg-secondary/50 p-1">
              <button
                type="button"
                onClick={() => setPeriod("weekly")}
                className={`rounded-md px-4 py-2 text-xs font-semibold ${period === "weekly" ? "bg-primary text-primary-foreground" : "text-muted-foreground"}`}
              >
                Current / weekly
              </button>
              <button
                type="button"
                onClick={() => setPeriod("historical")}
                className={`rounded-md px-4 py-2 text-xs font-semibold ${period === "historical" ? "bg-primary text-primary-foreground" : "text-muted-foreground"}`}
              >
                Historical
              </button>
            </div>
            {areas.length > 0 && (
              <div className="flex flex-wrap gap-2 text-[11px]">
                <span className="rounded-full bg-green-500/15 px-2.5 py-1 text-green-300">{summary.available} available</span>
                {summary.stale > 0 && <span className="rounded-full bg-amber-500/15 px-2.5 py-1 text-amber-300">{summary.stale} stale</span>}
                <span className="rounded-full bg-slate-500/15 px-2.5 py-1 text-slate-300">{summary.unavailable} unavailable</span>
              </div>
            )}
          </div>
        </section>

        {!region.adm0Gid && (
          <section className="card-glass p-10 text-center">
            <ShieldCheck className="mx-auto mb-4 h-10 w-10 text-primary" />
            <h2 className="text-xl font-semibold">Select a country to load safety data</h2>
            <p className="mt-2 text-sm text-muted-foreground">Choose a governorate to display its district boundaries on the map.</p>
          </section>
        )}

        {loadState.status === "loading" && (
          <section className="card-glass p-10 text-center text-sm text-muted-foreground">
            Loading safety scores and district boundaries…
          </section>
        )}

        {loadState.status === "error" && (
          <section role="alert" className="rounded-2xl border border-red-500/40 bg-red-500/10 p-6">
            <div className="flex items-start gap-3">
              <AlertTriangle className="mt-0.5 h-5 w-5 text-red-300" />
              <div className="flex-1">
                <h2 className="font-semibold text-red-200">Safety data is unavailable</h2>
                <p className="mt-1 text-sm text-red-200/80">{loadState.message}</p>
              </div>
              <button
                type="button"
                onClick={() => setRetryVersion((value) => value + 1)}
                className="flex items-center gap-1 rounded-lg border border-red-300/30 px-3 py-2 text-xs text-red-100"
              >
                <RefreshCw className="h-3.5 w-3.5" /> Retry
              </button>
            </div>
          </section>
        )}

        {loadState.status === "ready" && region.adm0Gid && (
          <>
            {geometryError && (
              <p role="alert" className="mb-4 rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-xs text-amber-200">
                Scores loaded, but the map overlay could not be loaded: {geometryError}
              </p>
            )}

            {region.adm1Gid ? (
              mapAreas.length > 0
                ? <SafetyMap areas={mapAreas} selectedAdm2Gid={selectedAdm2Gid} onSelect={setSelectedAdm2Gid} />
                : (
                  <section className="card-glass p-8 text-center text-sm text-muted-foreground">
                    No district geometry is available for this selection.
                  </section>
                )
            ) : (
              <section className="card-glass p-6 text-center text-sm text-muted-foreground">
                Country scores are loaded. Select a governorate to draw detailed safety overlays.
              </section>
            )}

            <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_22rem]">
              <section className="card-glass p-5">
                <h2 className="mb-4 text-lg font-semibold">District scores</h2>
                {areas.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No districts were returned for this region.</p>
                ) : (
                  <div className="grid max-h-[34rem] gap-2 overflow-y-auto pr-1 sm:grid-cols-2">
                    {areas.map((area) => {
                      const score = getScoreForPeriod(area, period);
                      const state = getSafetyDataState(score, period);
                      return (
                        <button
                          key={area.adm2Gid}
                          type="button"
                          onClick={() => setSelectedAdm2Gid(area.adm2Gid)}
                          className={`flex items-center justify-between rounded-xl border p-3 text-left transition ${
                            selectedAdm2Gid === area.adm2Gid
                              ? "border-primary bg-primary/10"
                              : "border-border/50 bg-secondary/25 hover:bg-secondary/50"
                          }`}
                        >
                          <span>
                            <strong className="block text-sm">{area.nameEn}</strong>
                            {area.nameAr && <span className="text-[11px] text-muted-foreground">{area.nameAr}</span>}
                          </span>
                          <span className="text-right">
                            <strong style={{ color: getSafetyColor(score.score) }}>{score.score ?? "—"}</strong>
                            <small className="block text-[9px] uppercase text-muted-foreground">{state}</small>
                          </span>
                        </button>
                      );
                    })}
                  </div>
                )}
              </section>

              <aside className="space-y-4">
                {selectedArea ? (
                  <>
                    <div>
                      <h2 className="text-xl font-bold">{selectedArea.nameEn}</h2>
                      {selectedArea.nameAr && <p className="text-sm text-muted-foreground">{selectedArea.nameAr}</p>}
                    </div>
                    <SafetyScorePanel title="Current safety" score={selectedArea.weeklyScore} period="weekly" />
                    <SafetyScorePanel title="Historical safety" score={selectedArea.historicalScore} period="historical" />
                  </>
                ) : (
                  <div className="card-glass p-6 text-sm text-muted-foreground">Select a district to inspect both score periods.</div>
                )}
              </aside>
            </div>
          </>
        )}
      </main>
      <Footer />
    </div>
  );
};

export default WhereToGo;
