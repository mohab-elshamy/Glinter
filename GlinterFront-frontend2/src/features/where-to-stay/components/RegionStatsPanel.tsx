import { useEffect, useState } from "react";
import { BarChart3 } from "lucide-react";
import { regionsApi } from "@/shared/services/api-regions";
import type { LoadState } from "@/shared/types/async-state";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import { formatUsdPrice } from "@/shared/lib/price";
import type { StayRegionStatsDto } from "@/shared/types/api";

interface RegionStatsPanelProps {
  stats: StayRegionStatsDto[];
  state: LoadState;
  region: RegionHierarchyGids;
  onSelectRegion: (region: RegionHierarchyGids) => void;
}

const RegionStatsPanel = ({
  stats,
  state,
  region,
  onSelectRegion,
}: RegionStatsPanelProps) => {
  const [names, setNames] = useState<Record<number, string>>({});
  const groupBy = stats[0]?.groupBy;

  useEffect(() => {
    let active = true;
    const request = groupBy === "Adm0"
      ? regionsApi.getCountries()
      : groupBy === "Adm1"
        ? regionsApi.getGovernorates(region.adm0Gid)
        : groupBy === "Adm2"
          ? regionsApi.getDistricts(region.adm1Gid)
          : groupBy === "Adm3"
            ? regionsApi.getNeighbourhoods(region.adm2Gid)
            : Promise.resolve([]);

    request.then((items) => {
      if (!active) return;
      setNames(Object.fromEntries(items.map((item) => [
        item.gid,
        item.nameEn || item.nameAr || item.pcode,
      ])));
    }).catch(() => {
      if (active) setNames({});
    });
    return () => {
      active = false;
    };
  }, [groupBy, region.adm0Gid, region.adm1Gid, region.adm2Gid]);

  const select = (stat: StayRegionStatsDto) => {
    if (stat.groupBy === "Adm0") onSelectRegion({ adm0Gid: stat.regionGid });
    if (stat.groupBy === "Adm1") onSelectRegion({ adm0Gid: region.adm0Gid, adm1Gid: stat.regionGid });
    if (stat.groupBy === "Adm2") onSelectRegion({ adm0Gid: region.adm0Gid, adm1Gid: region.adm1Gid, adm2Gid: stat.regionGid });
    if (stat.groupBy === "Adm3") onSelectRegion({ ...region, adm3Gid: stat.regionGid });
  };

  return (
    <section className="card-glass mb-6 p-4">
      <div className="mb-3 flex items-center gap-2">
        <BarChart3 className="h-4 w-4 text-accent" />
        <h3 className="text-sm font-semibold">Live regional stay statistics</h3>
      </div>
      {state.status === "loading" ? (
        <p className="text-xs text-muted-foreground">Calculating region prices and listing counts…</p>
      ) : state.status === "error" ? (
        <p role="alert" className="text-xs text-destructive">{state.message}</p>
      ) : stats.length === 0 ? (
        <p className="text-xs text-muted-foreground">No region statistics match these filters.</p>
      ) : (
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          {stats.slice(0, 12).map((stat) => (
            <button
              key={`${stat.groupBy}-${stat.regionGid}`}
              type="button"
              onClick={() => select(stat)}
              className="rounded-xl border border-border/50 bg-secondary/30 p-3 text-left hover:border-accent/50"
            >
              <div className="flex items-start justify-between gap-2">
                <strong className="text-xs">{names[stat.regionGid] || `Region ${stat.regionGid}`}</strong>
                <span className="text-[10px] text-muted-foreground">{stat.hotelsCount} stays</span>
              </div>
              <p className="mt-2 text-sm font-bold text-accent">
                {stat.averagePrice == null ? "No price" : `${formatUsdPrice(stat.averagePrice)} avg.`}
              </p>
              <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-background/70">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-green-500 via-amber-400 to-red-500"
                  style={{ width: `${Math.max(4, stat.pricePercentage ?? 0)}%` }}
                />
              </div>
            </button>
          ))}
        </div>
      )}
    </section>
  );
};

export default RegionStatsPanel;
