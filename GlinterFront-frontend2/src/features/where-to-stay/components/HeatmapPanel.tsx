import { Shield, DollarSign, Armchair } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import LeafletMap from "@/components/LeafletMap";
import type { HeatmapLayer, Neighborhood, Hotel } from "../types";

interface HeatmapPanelProps {
  mapData: {
    markers: { lat: number; lng: number; name: string; cheapestPrice?: number; data: unknown }[];
    selectedMarker: { lat: number; lng: number; name: string; cheapestPrice?: number; data: unknown } | null;
    comparisonMarkers: { lat: number; lng: number; name: string; cheapestPrice?: number; data: unknown }[];
  };
  backgroundLayer: HeatmapLayer | "none";
  selectedNeighborhood: Neighborhood | null;
  isComparing: boolean;
  comparisonNeighborhoods: Neighborhood[];
  filteredHotels: Hotel[];
  onSelectNeighborhood: (n: Neighborhood | null) => void;
  onToggleNeighborhoodForComparison: (n: Neighborhood) => void;
  showMarkers?: boolean;
  height?: string;
}

const HeatmapPanel = ({
  mapData,
  backgroundLayer,
  selectedNeighborhood,
  isComparing,
  comparisonNeighborhoods,
  filteredHotels,
  onSelectNeighborhood,
  onToggleNeighborhoodForComparison,
  showMarkers = true,
  height,
}: HeatmapPanelProps) => {
  return (
    <div className="w-full h-full relative">
      <LeafletMap
        center={[27, 29.9]}
        zoom={selectedNeighborhood || comparisonNeighborhoods.length > 0 ? 12 : 5.49}
        markers={mapData.markers}
        onMarkerClick={(marker) => {
          if (marker.data && "safety" in (marker.data as Record<string, unknown>)) {
            const n = marker.data as Neighborhood;
            if (isComparing) {
              onToggleNeighborhoodForComparison(n);
            } else {
              onSelectNeighborhood(
                selectedNeighborhood?.name === marker.name ? null : n
              );
            }
          }
        }}
        selectedMarker={mapData.selectedMarker}
        comparisonMarkers={mapData.comparisonMarkers}
        showMarkers={showMarkers}
        height={height || "100%"}
        showSearch={true}
        showFullscreen={true}
        heatmapLayerType={backgroundLayer !== "none" ? backgroundLayer : undefined}
        comparisonNeighborhoods={comparisonNeighborhoods}
      />

      <AnimatePresence>
        {selectedNeighborhood && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 20 }}
            className="absolute bottom-6 left-6 liquid-glass rounded-2xl p-4 max-w-xs z-[100]"
          >
            <h4 className="font-bold text-sm mb-2">{selectedNeighborhood.name}</h4>
            <div className="grid grid-cols-3 gap-3 text-xs">
              <div className="text-center">
                <Shield className="w-4 h-4 mx-auto mb-1 text-green-400" />
                <p className="font-bold">{selectedNeighborhood.safety}%</p>
                <p className="text-gray-400">Safety</p>
              </div>
              <div className="text-center">
                <DollarSign className="w-4 h-4 mx-auto mb-1 text-brand-gold" />
                <p className="font-bold">{selectedNeighborhood.price}/100</p>
                <p className="text-gray-400">Price Level</p>
              </div>
              <div className="text-center">
                <Armchair className="w-4 h-4 mx-auto mb-1 text-brand-teal" />
                <p className="font-bold">{selectedNeighborhood.comfort}%</p>
                <p className="text-gray-400">Comfort</p>
              </div>
            </div>
            <p className="text-xs text-gray-400 mt-2 text-center">
              {filteredHotels.length} hotels in {selectedNeighborhood.name}
            </p>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
};

export default HeatmapPanel;
