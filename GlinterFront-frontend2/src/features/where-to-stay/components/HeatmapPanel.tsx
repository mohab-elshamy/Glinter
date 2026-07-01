import LeafletMap from "@/components/LeafletMap";
import type { Hotel } from "../types";

interface HeatmapPanelProps {
  mapData: {
    markers: { lat: number; lng: number; name: string; cheapestPrice?: number; data: Hotel }[];
    selectedMarker: { lat: number; lng: number; name: string; cheapestPrice?: number; data: Hotel } | null;
    comparisonMarkers: Array<{ lat: number; lng: number; name: string; cheapestPrice?: number; data: Hotel }>;
  };
  onSelectHotel: (hotel: Hotel) => void;
  onMapClick: (lat: number, lng: number) => void;
  showMarkers?: boolean;
  height?: string;
}

const HeatmapPanel = ({
  mapData,
  onSelectHotel,
  onMapClick,
  showMarkers = true,
  height,
}: HeatmapPanelProps) => (
  <div className="relative h-full w-full">
    <LeafletMap
      center={[27, 29.9]}
      zoom={5.49}
      markers={mapData.markers}
      onMarkerClick={(marker) => {
        if (marker.data) onSelectHotel(marker.data as Hotel);
      }}
      onMapClick={onMapClick}
      selectedMarker={mapData.selectedMarker}
      comparisonMarkers={mapData.comparisonMarkers}
      showMarkers={showMarkers}
      height={height || "100%"}
      showSearch
      showFullscreen
    />
  </div>
);

export default HeatmapPanel;
