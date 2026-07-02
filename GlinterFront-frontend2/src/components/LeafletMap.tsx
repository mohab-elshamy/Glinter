import { useEffect, useRef } from "react";
import {
  Circle,
  GeoJSON,
  MapContainer,
  Marker,
  Polyline,
  TileLayer,
  Tooltip,
  useMap,
  useMapEvents,
} from "react-leaflet";
import L from "leaflet";
import type { Feature, GeoJsonObject, Geometry } from "geojson";
import "leaflet.markercluster";
import "leaflet-geosearch/dist/geosearch.css";
import { GeoSearchControl, OpenStreetMapProvider } from "leaflet-geosearch";
import { formatUsdPrice } from "@/shared/lib/price";
import { overlayLayers } from "@/shared/lib/overlay-layers";

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

const CurrentLocationIcon = L.divIcon({
  className: "current-location-marker",
  html: `<div style="
    position:relative;width:24px;height:24px;
    display:flex;align-items:center;justify-content:center;
  ">
    <span style="
      position:absolute;width:24px;height:24px;border-radius:9999px;
      background:rgba(14,165,233,.28);box-shadow:0 0 0 8px rgba(14,165,233,.12);
    "></span>
    <span style="
      position:relative;width:14px;height:14px;border-radius:9999px;
      border:3px solid white;background:#0ea5e9;
      box-shadow:0 2px 12px rgba(14,165,233,.8);
    "></span>
  </div>`,
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

export interface MapMarker {
  id?: number;
  lat: number;
  lng: number;
  name: string;
  cheapestPrice?: number;
  color?: string;
  label?: string;
  data?: Partial<{
    price: number | null;
    rating: number;
    area: string;
    comfortScore: number;
    safetyScore: number;
  }>;
}

export interface MapOverlayArea {
  id: number;
  name: string;
  value?: number | null;
  color: string;
  geometry: Geometry | Feature;
  fillOpacity?: number;
  dashArray?: string;
}

export interface MapRouteLine {
  id: string;
  positions: Array<[number, number]>;
  color?: string;
  dashed?: boolean;
  label?: string;
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
  overlayAreas?: MapOverlayArea[];
  currentLocation?: {
    latitude: number;
    longitude: number;
    accuracy?: number;
  };
  recenterSequence?: number;
  routeLines?: MapRouteLine[];
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

const escapeMapHtml = (value: string) => value
  .replaceAll("&", "&amp;")
  .replaceAll("<", "&lt;")
  .replaceAll(">", "&gt;")
  .replaceAll('"', "&quot;")
  .replaceAll("'", "&#039;");

const toGeoJson = (area: MapOverlayArea): GeoJsonObject => {
  if ("type" in area.geometry && area.geometry.type === "Feature") {
    return area.geometry as Feature;
  }
  return {
    type: "Feature",
    properties: { id: area.id },
    geometry: area.geometry as Geometry,
  } satisfies Feature;
};

// Component to handle markers, clustering larger datasets for responsiveness.
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
  const markerLayerRef = useRef<L.LayerGroup | null>(null);

  useEffect(() => {
    if (markerLayerRef.current) {
      map.removeLayer(markerLayerRef.current);
      markerLayerRef.current = null;
    }
    const markerLayer: L.LayerGroup = markers.length > 50
      ? L.markerClusterGroup({ chunkedLoading: true, maxClusterRadius: 55 })
      : L.layerGroup();

    markers.forEach((marker) => {
      const isSelected = selectedMarker?.name === marker.name;
      const isInComparison = comparisonMarkers?.some((m) => m.name === marker.name);

      let markerColor = marker.color || BRAND_PURPLE;
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
      const displayPrice = marker.label ??
        (cp !== undefined && cp !== null ? formatUsdPrice(cp) : "");
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
          ">${escapeMapHtml(displayPrice)}</span>
        </div>`,
        iconSize: [32, 36],
        iconAnchor: [16, 18],
      });

      const leafletMarker = L.marker([marker.lat, marker.lng], { icon: markerIcon });

      if (marker.name) {
        // Create detailed popup content
        let popupContent = `<div style="min-width: 200px;"><strong style="font-size: 14px;">${escapeMapHtml(marker.name)}</strong>`;
        
        if (marker.data) {
          const data = marker.data;
          if (data.area) {
            popupContent += `
              <div style="margin-top: 8px; font-size: 12px;">
                <div style="margin: 4px 0;">📍 ${escapeMapHtml(data.area)}</div>
            `;
          }
          if (data.rating != null) {
            popupContent += `
                <div style="display: flex; justify-content: space-between; margin: 4px 0;">
                  <span>⭐ Rating:</span>
                  <strong>${data.rating}</strong>
                </div>
            `;
          }
          if (data.price != null) {
            popupContent += `
                <div style="display: flex; justify-content: space-between; margin: 4px 0;">
                  <span>💰 Price:</span>
                  <strong>${escapeMapHtml(formatUsdPrice(data.price))}/night</strong>
                </div>
            `;
          }
          if (data.comfortScore != null) {
            popupContent += `
                <div style="display: flex; justify-content: space-between; margin: 4px 0;">
                  <span>✨ Comfort:</span>
                  <strong>${data.comfortScore}/100</strong>
                </div>
            `;
          }
          if (data.safetyScore != null) {
            popupContent += `
                <div style="display: flex; justify-content: space-between; margin: 4px 0;">
                  <span>🛡️ Safety:</span>
                  <strong>${data.safetyScore}/100</strong>
                </div>
            `;
          }
          if (data.area) popupContent += "</div>";
        }
        popupContent += `</div>`;
        leafletMarker.bindPopup(popupContent);
      }

      if (onMarkerClick) {
        leafletMarker.on("click", () => {
          onMarkerClick(marker);
        });
      }

      markerLayer.addLayer(leafletMarker);
    });
    markerLayer.addTo(map);
    markerLayerRef.current = markerLayer;

    return () => {
      map.removeLayer(markerLayer);
      markerLayerRef.current = null;
    };
  }, [map, markers, onMarkerClick, selectedMarker, comparisonMarkers]);

  return null;
}

function OverlayBounds({ areas }: { areas: MapOverlayArea[] }) {
  const map = useMap();

  useEffect(() => {
    if (areas.length === 0) return;
    const group = L.geoJSON({
      type: "FeatureCollection",
      features: areas.map((area) => toGeoJson(area) as Feature),
    });
    const bounds = group.getBounds();
    if (bounds.isValid()) map.fitBounds(bounds, { padding: [32, 32], maxZoom: 11 });
  }, [areas, map]);

  return null;
}

function CurrentLocationLayer({
  location,
  recenterSequence,
}: {
  location?: LeafletMapProps["currentLocation"];
  recenterSequence?: number;
}) {
  const map = useMap();

  useEffect(() => {
    if (!location || !recenterSequence) return;
    map.setView([location.latitude, location.longitude], Math.max(map.getZoom(), 14), {
      animate: true,
    });
  }, [location, map, recenterSequence]);

  if (!location) return null;
  const center: [number, number] = [location.latitude, location.longitude];

  return (
    <>
      {location.accuracy != null && (
        <Circle
          center={center}
          radius={Math.max(20, location.accuracy)}
          pathOptions={{
            color: "#38bdf8",
            fillColor: "#38bdf8",
            fillOpacity: 0.08,
            opacity: 0.35,
            weight: 1,
          }}
        />
      )}
      <Marker position={center} icon={CurrentLocationIcon} zIndexOffset={2000}>
        <Tooltip direction="top" offset={[0, -8]} permanent>You are here</Tooltip>
      </Marker>
    </>
  );
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

function RouteBounds({
  routeLines,
  markers,
}: {
  routeLines: MapRouteLine[];
  markers: MapMarker[];
}) {
  const map = useMap();
  useEffect(() => {
    const points = [
      ...routeLines.flatMap((line) => line.positions),
      ...markers.map((marker) => [marker.lat, marker.lng] as [number, number]),
    ];
    if (points.length < 2) return;
    const bounds = L.latLngBounds(points);
    if (bounds.isValid()) map.fitBounds(bounds, { padding: [36, 36], maxZoom: 14 });
  }, [map, markers, routeLines]);
  return null;
}

// Marker legend for the map
function MarkerLegend() {
  const items = [
    { color: "#9333ea", label: "Backend stay" },
    { color: "#FFD700", label: "Selected" },
  ];
  return (
    <div className={overlayLayers.mapOverlay} style={{
      position: "absolute",
      bottom: 16,
      left: 16,
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
  overlayAreas = [],
  currentLocation,
  recenterSequence,
  routeLines = [],
}) => {
  return (
    <div
      style={{ height, width: "100%" }}
      className={`${overlayLayers.map} relative isolate overflow-hidden rounded-lg border border-border/30`}
    >
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
        <OverlayBounds areas={overlayAreas} />
        {overlayAreas.map((area) => (
          <GeoJSON
            key={`${area.id}-${area.value ?? "none"}-${area.color}`}
            data={toGeoJson(area)}
            onEachFeature={(_feature, layer) => {
              const tooltip = document.createElement("span");
              tooltip.textContent = `${area.name}: ${area.value ?? "No data"}`;
              layer.bindTooltip(tooltip);
            }}
            style={{
              color: area.color,
              fillColor: area.color,
              fillOpacity: area.fillOpacity ?? 0.52,
              opacity: 0.95,
              weight: 2,
              dashArray: area.dashArray,
            }}
          />
        ))}
        <CurrentLocationLayer
          location={currentLocation}
          recenterSequence={recenterSequence}
        />
        <RouteBounds routeLines={routeLines} markers={markers} />
        {routeLines.map((line, index) => (
          <Polyline
            key={line.id}
            positions={line.positions}
            pathOptions={{
              color: line.color ?? ["#8b5cf6", "#0ea5e9", "#f59e0b", "#10b981"][index % 4],
              weight: 4,
              opacity: 0.8,
              dashArray: line.dashed ? "7 7" : undefined,
            }}
          >
            {line.label && <Tooltip sticky>{line.label}</Tooltip>}
          </Polyline>
        ))}
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
