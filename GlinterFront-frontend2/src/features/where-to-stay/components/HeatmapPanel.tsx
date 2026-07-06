import { MapPin, Star, X } from "lucide-react";
import LeafletMap, { type MapOverlayArea } from "@/components/LeafletMap";
import { formatUsdPrice } from "@/shared/lib/price";
import type { Hotel } from "../types";
import hotelImg from "@/assets/hotel-1.jpg";
import { overlayLayers } from "@/shared/lib/overlay-layers";
import type { ComfortIndexResponse } from "@/shared/types/comfort-index";
import type { PriceIndexResponse } from "@/shared/types/price-index";
import {
  getComfortScore,
  getPriceColor,
  getScoreColor,
  type StayMapMode,
} from "../map-modes";

interface HeatmapPanelProps {
  hotels: Hotel[];
  selectedHotel: Hotel | null;
  mode: StayMapMode;
  averagePrice?: number;
  comfortIndex?: ComfortIndexResponse | null;
  comfortAreas: MapOverlayArea[];
  priceIndex?: PriceIndexResponse | null;
  priceAreas: MapOverlayArea[];
  safetyScores: Map<number, number | null | undefined>;
  safetyAreas: MapOverlayArea[];
  currentLocation?: {
    latitude: number;
    longitude: number;
    accuracy?: number;
  };
  recenterSequence?: number;
  onSelectHotel: (hotel: Hotel) => void;
  onClearHotel: () => void;
  onViewDetails: (hotel: Hotel) => void;
  onMapClick: (lat: number, lng: number) => void;
  height?: string;
}

const legendByMode: Record<StayMapMode, Array<{ color: string; label: string }>> = {
  standard: [
    { color: "#9333ea", label: "Backend stay" },
    { color: "#FFD700", label: "Selected stay" },
  ],
  safety: [
    { color: "#22c55e", label: "80–100 safer signal" },
    { color: "#84cc16", label: "60–79" },
    { color: "#f59e0b", label: "40–59" },
    { color: "#ef4444", label: "0–39" },
    { color: "#64748b", label: "Unavailable" },
  ],
  comfort: [
    { color: "#22c55e", label: "Index above 125" },
    { color: "#84cc16", label: "Index 100–125" },
    { color: "#f59e0b", label: "Index 75–99" },
    { color: "#ef4444", label: "Index below 75" },
    { color: "#64748b", label: "No rated data" },
  ],
  price: [
    { color: "#22c55e", label: "Index 75 or lower" },
    { color: "#84cc16", label: "Index 76–100" },
    { color: "#f59e0b", label: "Index 101–125" },
    { color: "#ef4444", label: "Index above 125" },
    { color: "#64748b", label: "No priced data" },
  ],
};

const markerLabel = (
  hotel: Hotel,
  mode: StayMapMode,
  safetyScore?: number | null,
) => {
  if (mode === "safety") return safetyScore == null ? "—" : String(Math.round(safetyScore));
  if (mode === "comfort") return String(getComfortScore(hotel));
  return hotel.price == null ? "—" : formatUsdPrice(hotel.price);
};

const HeatmapPanel = ({
  hotels,
  selectedHotel,
  mode,
  averagePrice,
  comfortIndex,
  comfortAreas,
  priceIndex,
  priceAreas,
  safetyScores,
  safetyAreas,
  currentLocation,
  recenterSequence,
  onSelectHotel,
  onClearHotel,
  onViewDetails,
  onMapClick,
  height = "70vh",
}: HeatmapPanelProps) => {
  const markers = hotels
    .filter((hotel) => hotel.latitude != null && hotel.longitude != null)
    .map((hotel) => {
      const safetyScore = hotel.adm2Gid == null
        ? undefined
        : safetyScores.get(hotel.adm2Gid);
      const comfortScore = getComfortScore(hotel);
      const color = mode === "safety"
        ? getScoreColor(safetyScore)
        : mode === "comfort"
          ? getScoreColor(comfortScore)
          : mode === "price"
            ? getPriceColor(hotel.price, averagePrice)
            : "#9333ea";

      return {
        id: hotel.id,
        lat: hotel.latitude as number,
        lng: hotel.longitude as number,
        name: hotel.name,
        color,
        label: markerLabel(hotel, mode, safetyScore),
        cheapestPrice: hotel.price,
        data: {
          price: hotel.price,
          rating: hotel.rating,
          area: hotel.area,
          comfortScore,
          safetyScore: safetyScore ?? undefined,
        },
      };
    });

  const selectedMarker = selectedHotel?.latitude != null && selectedHotel.longitude != null
    ? markers.find((marker) => marker.id === selectedHotel.id) ?? null
    : null;

  return (
    <div className={`${overlayLayers.map} relative isolate h-full w-full overflow-hidden rounded-2xl border border-white/10`}>
      <LeafletMap
        center={[27, 29.9]}
        zoom={5.49}
        markers={markers}
        onMarkerClick={(marker) => {
          const hotel = hotels.find((item) => item.id === marker.id);
          if (hotel) onSelectHotel(hotel);
        }}
        onMapClick={onMapClick}
        selectedMarker={selectedMarker}
        overlayAreas={mode === "safety"
          ? safetyAreas
          : mode === "price"
            ? priceAreas
            : mode === "comfort"
              ? comfortAreas
              : []}
        currentLocation={currentLocation}
        recenterSequence={recenterSequence}
        showLegend={false}
        height={height}
        showSearch
        showFullscreen
      />

      <div
        data-testid="map-legend"
        className={`${overlayLayers.mapOverlay} pointer-events-none absolute left-3 top-3 max-h-[45%] max-w-[min(17rem,calc(100%-1.5rem))] overflow-y-auto rounded-xl border border-white/10 bg-black/85 p-2.5 text-[10px] shadow-xl backdrop-blur sm:bottom-4 sm:left-4 sm:top-auto sm:max-h-none sm:max-w-[min(19rem,calc(100%-2rem))] sm:p-3 sm:text-[11px]`}
      >
        <p className="mb-2 font-semibold capitalize text-white">{mode} map</p>
        <div className="space-y-1 text-gray-300">
          {legendByMode[mode].map((item) => (
            <div key={item.label} className="flex items-center gap-2">
              <span className="h-2.5 w-2.5 shrink-0 rounded-full" style={{ backgroundColor: item.color }} />
              <span>{item.label}</span>
            </div>
          ))}
          {mode === "safety" && <p className="border-t border-white/10 pt-1 text-gray-400">Dashed boundary = stale score</p>}
          {mode === "comfort" && (
            <div className="border-t border-white/10 pt-1 text-gray-400">
              <p>Combined index: {comfortIndex?.combined.indexValue == null ? "Unavailable" : Math.round(comfortIndex.combined.indexValue)}</p>
              <p>Combined avg: {comfortIndex?.combined.averageScore == null ? "Unavailable" : `${comfortIndex.combined.averageScore.toFixed(2)} score`}</p>
              <p>Formula: rating x log(reviews).</p>
            </div>
          )}
          {mode === "price" && (
            <div className="border-t border-white/10 pt-1 text-gray-400">
              <p>Combined index: {priceIndex?.combined.indexValue == null ? "Unavailable" : Math.round(priceIndex.combined.indexValue)}</p>
              <p>Combined avg: {formatUsdPrice(priceIndex?.combined.averagePrice ?? averagePrice, "Unavailable")}</p>
              <p>Markers show hotel nightly price.</p>
            </div>
          )}
          {currentLocation && (
            <div className="flex items-center gap-2 border-t border-white/10 pt-1 text-sky-300">
              <span className="h-2.5 w-2.5 rounded-full border-2 border-white bg-sky-500" />
              <span>You are here</span>
            </div>
          )}
        </div>
      </div>

      {selectedHotel && (
        <article
          data-testid="selected-stay-preview"
          className={`${overlayLayers.mapOverlay} absolute bottom-3 left-3 right-3 max-h-[45%] overflow-y-auto rounded-2xl border border-white/10 bg-[#111218]/95 shadow-2xl backdrop-blur sm:bottom-4 sm:left-auto sm:right-4 sm:w-[min(22rem,calc(100%-2rem))]`}
        >
          <button
            type="button"
            onClick={onClearHotel}
            aria-label="Close hotel preview"
            className="absolute right-2 top-2 z-10 rounded-full bg-black/65 p-1.5 text-white"
          >
            <X className="h-4 w-4" />
          </button>
          <div className="grid grid-cols-[6.5rem_1fr]">
            <div className="min-h-32 bg-white/5">
              {selectedHotel.image ? (
                <img
                  src={selectedHotel.image}
                  alt=""
                  className="h-full w-full object-cover"
                  onError={(event) => {
                    event.currentTarget.onerror = null;
                    event.currentTarget.src = hotelImg;
                  }}
                />
              ) : (
                <div className="flex h-full items-center justify-center"><MapPin className="h-6 w-6 text-gray-500" /></div>
              )}
            </div>
            <div className="p-4">
              <h3 className="pr-6 font-bold text-white">{selectedHotel.name}</h3>
              <p className="mt-1 line-clamp-1 text-xs text-gray-400">{selectedHotel.area}</p>
              <div className="mt-2 flex items-center justify-between gap-2 text-xs">
                <span className="flex items-center gap-1 text-amber-300">
                  <Star className="h-3.5 w-3.5 fill-current" /> {selectedHotel.rating}
                </span>
                <strong className="text-brand-gold">
                  {formatUsdPrice(selectedHotel.price)}
                  {selectedHotel.price != null && selectedHotel.price > 0 ? "/night" : ""}
                </strong>
              </div>
              <button
                type="button"
                onClick={() => onViewDetails(selectedHotel)}
                className="mt-3 w-full rounded-lg bg-primary px-3 py-2 text-xs font-semibold text-primary-foreground"
              >
                View hotel details
              </button>
            </div>
          </div>
        </article>
      )}
    </div>
  );
};

export default HeatmapPanel;
