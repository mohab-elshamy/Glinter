import { useEffect, useRef } from "react";
import { MapContainer, TileLayer, useMap, useMapEvents } from "react-leaflet";
import L from "leaflet";
import "leaflet.markercluster";
import "leaflet-geosearch/dist/geosearch.css";
import { GeoSearchControl, OpenStreetMapProvider } from "leaflet-geosearch";

// Fix for default marker icons in React/Webpack
import icon from "leaflet/dist/images/marker-icon.png";
import iconShadow from "leaflet/dist/images/marker-shadow.png";
import iconRetina from "leaflet/dist/images/marker-icon-2x.png";

const DefaultIcon = L.icon({
  iconUrl: icon,
  iconRetinaUrl: iconRetina,
  shadowUrl: iconShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  tooltipAnchor: [16, -28],
  shadowSize: [41, 41],
});

L.Marker.prototype.options.icon = DefaultIcon;

interface MapMarker {
  lat: number;
  lng: number;
  name: string;
  cheapestPrice?: number;
  data?: Partial<{
    price: number;
    rating: number;
    area: string;
  }>;
}

interface LeafletMapProps {
  center: [number, number];
  zoom: number;
  markers?: MapMarker[];
  onMarkerClick?: (marker: MapMarker) => void;
  onMapClick?: (lat: number, lng: number) => void;
  selectedMarker?: MapMarker | null;
  comparisonMarkers?: MapMarker[];
  height?: string;
  showSearch?: boolean;
  showFullscreen?: boolean;
  showMarkers?: boolean;
  showLegend?: boolean;
}

function MapClickHandler({ onMapClick }: { onMapClick?: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (event) => onMapClick?.(event.latlng.lat, event.latlng.lng),
  });
  return null;
}

// Component to add search control
function SearchControl({ showSearch }: { showSearch?: boolean }) {
  const map = useMap();

  useEffect(() => {
    if (showSearch) {
      const provider = new OpenStreetMapProvider();
      const searchControl = GeoSearchControl({
        provider,
        style: "bar",
        searchLabel: "Search location...",
        showMarker: true,
        showPopup: false,
        marker: {
          icon: DefaultIcon,
        },
      }) as unknown as L.Control;

      map.addControl(searchControl);

      return () => {
        map.removeControl(searchControl);
      };
    }
  }, [map, showSearch]);

  return null;
}

// Component to add fullscreen control
function FullscreenControl({ showFullscreen }: { showFullscreen?: boolean }) {
  const map = useMap();
  const controlRef = useRef<L.Control | null>(null);

  useEffect(() => {
    let active = true;
    if (showFullscreen) {
      void import("leaflet.fullscreen").then((module) => {
        if (!active) return;
        const Fullscreen = module.default as unknown as new () => L.Control;
        const fullscreenControl = new Fullscreen();
        map.addControl(fullscreenControl);
        controlRef.current = fullscreenControl;
      }).catch((error: unknown) => {
        console.error("Could not load the fullscreen map control.", error);
      });
    }

    return () => {
      active = false;
      if (controlRef.current) {
        map.removeControl(controlRef.current);
        controlRef.current = null;
      }
    };
  }, [map, showFullscreen]);

  return null;
}

const BRAND_PURPLE = "#9333ea";
const SELECTED_GOLD = "#FFD700";
const COMPARISON_TEAL = "#00CED1";

// Component to handle markers (without clustering)
function MarkerGroup({
  markers,
  onMarkerClick,
  selectedMarker,
  comparisonMarkers,
}: {
  markers: MapMarker[];
  onMarkerClick?: (marker: MapMarker) => void;
  selectedMarker?: MapMarker | null;
  comparisonMarkers?: MapMarker[];
}) {
  const map = useMap();
  const markersRef = useRef<L.Marker[]>([]);

  useEffect(() => {
    markersRef.current.forEach((marker) => map.removeLayer(marker));
    markersRef.current = [];

    markers.forEach((marker) => {
      const isSelected = selectedMarker?.name === marker.name;
      const isInComparison = comparisonMarkers?.some((m) => m.name === marker.name);

      let markerColor = BRAND_PURPLE;
      let bgOpacity = "0.5";
      const hexSize = isSelected ? "scale(1.4)" : isInComparison ? "scale(1.2)" : "";

      if (isSelected) {
        markerColor = SELECTED_GOLD;
        bgOpacity = "0.8";
      } else if (isInComparison) {
        markerColor = COMPARISON_TEAL;
        bgOpacity = "0.8";
      }

      const cp = marker.cheapestPrice;
      const displayPrice = cp !== undefined && cp !== null ? `$${cp}` : "";
      const markerIcon = L.divIcon({
        className: `custom-marker ${isSelected || isInComparison ? "selected" : ""}`,
        html: `<div style="
          width: 32px;
          height: 36px;
          position: relative;
          display: flex;
          align-items: center;
          justify-content: center;
          ${hexSize}
          transition: transform 0.3s ease;
          filter: drop-shadow(0 0 4px ${markerColor});
        ">
          <div style="position:absolute;inset:0;background:${markerColor};opacity:${bgOpacity};clip-path:polygon(50% 0%,100% 25%,100% 75%,50% 100%,0% 75%,0% 25%);"></div>
          <div style="
            position:absolute;inset:2px;
            background:rgba(255,255,255,0.08);
            backdrop-filter:blur(4px);
            -webkit-backdrop-filter:blur(4px);
            clip-path:polygon(50% 0%,100% 25%,100% 75%,50% 100%,0% 75%,0% 25%);
            border:1px solid rgba(255,255,255,0.2)
          "></div>
          <span style="
            position:relative;z-index:2;
            color:white;font-size:10px;font-weight:700;
          ">${displayPrice}</span>
        </div>`,
        iconSize: [32, 36],
        iconAnchor: [16, 18],
      });

      const leafletMarker = L.marker([marker.lat, marker.lng], { icon: markerIcon });

      if (marker.name) {
        // Create detailed popup content
        let popupContent = `<div style="min-width: 200px;"><strong style="font-size: 14px;">${marker.name}</strong>`;
        
        if (marker.data) {
          const data = marker.data;
          if ("rating" in data) {
            const h = data as Required<Pick<NonNullable<MapMarker["data"]>, "area" | "rating" | "price">>;
            popupContent += `
              <div style="margin-top: 8px; font-size: 12px;">
                <div style="margin: 4px 0;">📍 ${h.area}</div>
                <div style="display: flex; justify-content: space-between; margin: 4px 0;">
                  <span>⭐ Rating:</span>
                  <strong>${h.rating}</strong>
                </div>
                <div style="display: flex; justify-content: space-between; margin: 4px 0;">
                  <span>💰 Price:</span>
                  <strong>$${h.price}/night</strong>
                </div>
              </div>
            `;
          }
        }
        popupContent += `</div>`;
        leafletMarker.bindPopup(popupContent);
      }

      if (onMarkerClick) {
        leafletMarker.on("click", () => {
          onMarkerClick(marker);
        });
      }

      leafletMarker.addTo(map);
      markersRef.current.push(leafletMarker);
    });

    return () => {
      markersRef.current.forEach((marker) => {
        map.removeLayer(marker);
      });
      markersRef.current = [];
    };
  }, [map, markers, onMarkerClick, selectedMarker, comparisonMarkers]);

  return null;
}

// Component to handle map bounds for comparison mode
function MapBounds({
  comparisonMarkers,
  selectedMarker,
  allMarkers,
}: {
  comparisonMarkers?: MapMarker[];
  selectedMarker?: MapMarker | null;
  allMarkers?: MapMarker[];
}) {
  const map = useMap();

  useEffect(() => {
    const markersToFit: MapMarker[] = [];
    
    if (comparisonMarkers && comparisonMarkers.length > 0) {
      // When comparing, fit all comparison markers
      markersToFit.push(...comparisonMarkers);
      // Also include any hotel markers in those neighborhoods
      if (allMarkers) {
        const hotelMarkers = allMarkers.filter(m => 
          m.data && "rating" in m.data && 
          comparisonMarkers.some(cm => {
            const hotelArea = m.data?.area;
            return cm.name === hotelArea;
          })
        );
        markersToFit.push(...hotelMarkers);
      }
    } else if (selectedMarker) {
      markersToFit.push(selectedMarker);
      // Include hotel markers in selected neighborhood
      if (allMarkers) {
        const hotelMarkers = allMarkers.filter(m => 
          m.data && "rating" in m.data && 
          m.data?.area === selectedMarker.name
        );
        markersToFit.push(...hotelMarkers);
      }
    }

    if (markersToFit.length > 0) {
      const bounds = L.latLngBounds(
        markersToFit.map((m) => [m.lat, m.lng] as [number, number])
      );
      map.fitBounds(bounds, { padding: [50, 50], maxZoom: comparisonMarkers && comparisonMarkers.length > 1 ? 12 : 13 });
    }
  }, [map, comparisonMarkers, selectedMarker, allMarkers]);

  return null;
}

// Marker legend for the map
function MarkerLegend() {
  const items = [
    { color: "#9333ea", label: "Backend stay" },
    { color: "#FFD700", label: "Selected" },
  ];
  return (
    <div style={{
      position: "absolute",
      bottom: 16,
      left: 16,
      zIndex: 1000,
      background: "rgba(11,12,16,0.85)",
      backdropFilter: "blur(12px)",
      WebkitBackdropFilter: "blur(12px)",
      border: "1px solid rgba(255,255,255,0.08)",
      borderRadius: 12,
      padding: "8px 12px",
      pointerEvents: "none",
    }}>
      {items.map((item) => (
        <div key={item.label} style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 4 }}>
          <div style={{
            width: 10, height: 10,
            clipPath: "polygon(50% 0%, 100% 25%, 100% 75%, 50% 100%, 0% 75%, 0% 25%)",
            background: item.color,
            flexShrink: 0,
          }} />
          <span style={{ fontSize: 10, color: "#9CA3AF", whiteSpace: "nowrap" }}>{item.label}</span>
        </div>
      ))}
    </div>
  );
}

const LeafletMap: React.FC<LeafletMapProps> = ({
  center,
  zoom,
  markers = [],
  onMarkerClick,
  onMapClick,
  selectedMarker,
  comparisonMarkers = [],
  height = "400px",
  showSearch = true,
  showFullscreen = true,
  showMarkers = true,
  showLegend = true,
}) => {
  return (
    <div style={{ position: "relative", height, width: "100%" }} className="rounded-lg overflow-hidden border border-border/30">
      <MapContainer
        center={center}
        zoom={zoom}
        style={{ height: "100%", width: "100%" }}
        scrollWheelZoom={true}
        zoomSnap={0.1}
        zoomDelta={0.5}
        wheelPxPerZoomLevel={120}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <SearchControl showSearch={showSearch} />
        <FullscreenControl showFullscreen={showFullscreen} />
        <MapClickHandler onMapClick={onMapClick} />
        <MapBounds 
          comparisonMarkers={comparisonMarkers} 
          selectedMarker={selectedMarker}
          allMarkers={markers}
        />
        {markers.length > 0 && showMarkers && (
          <MarkerGroup
            markers={markers}
            onMarkerClick={onMarkerClick}
            selectedMarker={selectedMarker}
            comparisonMarkers={comparisonMarkers}
          />
        )}
      </MapContainer>
      {showLegend && <MarkerLegend />}
    </div>
  );
};

export default LeafletMap;
