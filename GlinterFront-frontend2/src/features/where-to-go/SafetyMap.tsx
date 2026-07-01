import { useEffect, useMemo } from "react";
import { GeoJSON, MapContainer, TileLayer, useMap } from "react-leaflet";
import L from "leaflet";
import type { Feature, GeoJsonObject, Geometry } from "geojson";
import { getSafetyColor } from "@/shared/lib/safety";
import type { SafetyDataState } from "@/shared/types/safety";

export interface SafetyMapArea {
  adm2Gid: number;
  name: string;
  score?: number | null;
  state: SafetyDataState;
  geometry: Geometry | Feature;
}

interface SafetyMapProps {
  areas: SafetyMapArea[];
  selectedAdm2Gid?: number;
  onSelect: (adm2Gid: number) => void;
}

const toGeoJson = (area: SafetyMapArea): GeoJsonObject => {
  if ("type" in area.geometry && area.geometry.type === "Feature") {
    return area.geometry as Feature;
  }

  return {
    type: "Feature",
    properties: { adm2Gid: area.adm2Gid },
    geometry: area.geometry as Geometry,
  } satisfies Feature;
};

const MapBounds = ({ areas }: { areas: SafetyMapArea[] }) => {
  const map = useMap();

  useEffect(() => {
    if (areas.length === 0) return;
    const group = L.geoJSON({
      type: "FeatureCollection",
      features: areas.map((area) => toGeoJson(area) as Feature),
    });
    const bounds = group.getBounds();
    if (bounds.isValid()) map.fitBounds(bounds, { padding: [24, 24], maxZoom: 11 });
  }, [areas, map]);

  return null;
};

const SafetyMap = ({ areas, selectedAdm2Gid, onSelect }: SafetyMapProps) => {
  const boundsKey = useMemo(
    () => areas.map((area) => area.adm2Gid).sort((a, b) => a - b).join(","),
    [areas],
  );

  return (
    <div className="relative h-[32rem] overflow-hidden rounded-2xl border border-border/40">
      <MapContainer
        center={[27, 29.9]}
        zoom={5.4}
        className="h-full w-full"
        scrollWheelZoom
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <MapBounds key={boundsKey} areas={areas} />
        {areas.map((area) => {
          const selected = area.adm2Gid === selectedAdm2Gid;
          const color = getSafetyColor(area.score);
          return (
            <GeoJSON
              key={`${area.adm2Gid}-${area.score ?? "none"}-${area.state}-${selected}`}
              data={toGeoJson(area)}
              eventHandlers={{ click: () => onSelect(area.adm2Gid) }}
              onEachFeature={(_feature, layer) => {
                const tooltip = document.createElement("span");
                tooltip.textContent = `${area.name}: ${area.score ?? "No score"}`;
                layer.bindTooltip(tooltip);
              }}
              style={{
                color: selected ? "#ffffff" : color,
                fillColor: color,
                fillOpacity: area.state === "unavailable" ? 0.18 : 0.55,
                opacity: 0.95,
                weight: selected ? 4 : 2,
                dashArray: area.state === "stale" ? "7 5" : undefined,
              }}
            />
          );
        })}
      </MapContainer>
      <div className="pointer-events-none absolute bottom-4 left-4 z-[1000] rounded-xl border border-white/10 bg-black/80 p-3 text-[11px] backdrop-blur">
        <div className="mb-2 font-semibold text-white">Safety score</div>
        <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
          <span><i className="mr-1.5 inline-block h-2.5 w-2.5 rounded-full bg-green-500" />80–100</span>
          <span><i className="mr-1.5 inline-block h-2.5 w-2.5 rounded-full bg-lime-500" />60–79</span>
          <span><i className="mr-1.5 inline-block h-2.5 w-2.5 rounded-full bg-amber-500" />40–59</span>
          <span><i className="mr-1.5 inline-block h-2.5 w-2.5 rounded-full bg-red-500" />0–39</span>
          <span><i className="mr-1.5 inline-block h-2.5 w-2.5 rounded-full bg-slate-500" />Unavailable</span>
          <span className="border-b border-dashed border-white/70">Dashed = stale</span>
        </div>
      </div>
    </div>
  );
};

export default SafetyMap;
