import { useState, useMemo, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import type { HeatmapLayer, Neighborhood, Hotel } from "./types";
import { neighborhoods, allHotels } from "./data";
import { staysApi } from "@/shared/services/api-stays";
import { authStorage } from "@/shared/lib/auth";

export function useWhereToStay() {
  const [searchParams] = useSearchParams();
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedNeighborhood, setSelectedNeighborhood] = useState<Neighborhood | null>(null);
  const [maxPrice, setMaxPrice] = useState(200);
  const [minSafety, setMinSafety] = useState(0);
  const [minComfort, setMinComfort] = useState(0);
  const [minRating, setMinRating] = useState(0);
  const [backgroundLayer, setBackgroundLayer] = useState<HeatmapLayer | "none">("none");
  const [checkin, setCheckin] = useState("");
  const [checkout, setCheckout] = useState("");
  const [guests, setGuests] = useState(1);
  const [hasSearched, setHasSearched] = useState(false);
  const [isComparing, setIsComparing] = useState(false);
  const [comparisonNeighborhoods, setComparisonNeighborhoods] = useState<Neighborhood[]>([]);
  const [selectedHotel, setSelectedHotel] = useState<Hotel | null>(null);
  const [apiHotels, setApiHotels] = useState<Hotel[]>([]);

  useEffect(() => {
    const city = searchParams.get("city");
    if (city) {
      setSearchQuery(city);
      const matched = neighborhoods.find(n => n.name.toLowerCase() === city.toLowerCase());
      if (matched) {
        setSelectedNeighborhood(matched);
      }
    }
  }, [searchParams]);

  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;

    staysApi.getStays()
      .then(stays => {
        const mapped: Hotel[] = stays.map(s => ({
          name: s.name,
          area: s.address?.split(",").pop()?.trim() || "Unknown",
          rating: 4.0,
          reviews: 0,
          price: Math.round(s.pricePerNight),
          comfort: 50,
          safety: 50,
          amenities: s.amenities?.length > 0 ? s.amenities : [],
        }));
        setApiHotels(mapped);
      })
      .catch(() => {});
  }, []);

  const filteredHotels = useMemo(() => {
    let hotels = [...allHotels, ...apiHotels];

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      hotels = hotels.filter(
        (h) =>
          h.name.toLowerCase().includes(q) ||
          h.area.toLowerCase().includes(q)
      );
    }

    if (selectedNeighborhood) {
      hotels = hotels.filter(
        (h) => h.area.toLowerCase() === selectedNeighborhood.name.toLowerCase()
      );
    }

    hotels = hotels.filter((h) => h.price <= maxPrice && h.rating >= minRating && h.safety >= minSafety && h.comfort >= minComfort);

    if (backgroundLayer !== "none") {
      if (backgroundLayer === "safety") {
        hotels.sort((a, b) => b.safety - a.safety);
      } else if (backgroundLayer === "comfort") {
        hotels.sort((a, b) => b.comfort - a.comfort);
      } else {
        hotels.sort((a, b) => a.price - b.price);
      }
    }

    return hotels;
  }, [searchQuery, selectedNeighborhood, maxPrice, minRating, minSafety, minComfort, backgroundLayer]);

  const topRecommendations = useMemo(() => {
    const sorted = [...filteredHotels];
    if (backgroundLayer !== "none" && backgroundLayer !== "price") {
      sorted.sort((a, b) => b[backgroundLayer] - a[backgroundLayer]);
    }
    return sorted.slice(0, 2);
  }, [filteredHotels, backgroundLayer]);

  const handleSearch = () => {
    setHasSearched(true);
    setSelectedNeighborhood(null);
  };

  const toggleComparisonMode = () => {
    setIsComparing(!isComparing);
    if (!isComparing) {
      setComparisonNeighborhoods([]);
      setSelectedNeighborhood(null);
    }
  };

  const toggleNeighborhoodForComparison = (neighborhood: Neighborhood) => {
    if (!isComparing) return;

    setComparisonNeighborhoods((prev) => {
      const exists = prev.find((n) => n.name === neighborhood.name);
      if (exists) {
        return prev.filter((n) => n.name !== neighborhood.name);
      } else if (prev.length < 4) {
        return [...prev, neighborhood];
      } else {
        return prev;
      }
    });
  };

  const removeFromComparison = (name: string) => {
    setComparisonNeighborhoods((prev) => prev.filter((n) => n.name !== name));
  };

  const mapData = useMemo(() => {
    let filteredNeighborhoods = neighborhoods;

    // Strict AND filters: remove any dot that fails ANY active filter
    if (maxPrice < 200) {
      filteredNeighborhoods = filteredNeighborhoods.filter(n => n.price <= maxPrice);
    }
    if (minSafety > 0) {
      filteredNeighborhoods = filteredNeighborhoods.filter(n => n.safety >= minSafety);
    }
    if (minComfort > 0) {
      filteredNeighborhoods = filteredNeighborhoods.filter(n => n.comfort >= minComfort);
    }

    if (filteredNeighborhoods.length === 0) {
      return {
        heatmapPoints: [],
        markers: [],
        selectedMarker: null,
        comparisonMarkers: [],
        isFilteredOut: true,
      };
    }

    // For each neighborhood, find the cheapest hotel price in that area
    const getCheapestPrice = (areaName: string, fallbackIndex: number): number => {
      const hotelsInArea = allHotels.filter((h) => h.area === areaName);
      if (hotelsInArea.length > 0) {
        return Math.min(...hotelsInArea.map((h) => h.price));
      }
      // Fallback: convert 0-100 index to dollar estimate
      return Math.round(fallbackIndex * 2);
    };

    // Markers: solid color, no per-dot logic needed
    const neighborhoodMarkers = filteredNeighborhoods.map((n) => ({
      lat: n.lat,
      lng: n.lng,
      name: n.name,
      cheapestPrice: getCheapestPrice(n.name, n.price),
      data: n,
    }));

    const selectedMarker = selectedNeighborhood
      ? {
          lat: selectedNeighborhood.lat,
          lng: selectedNeighborhood.lng,
          name: selectedNeighborhood.name,
          cheapestPrice: getCheapestPrice(selectedNeighborhood.name, selectedNeighborhood.price),
          data: selectedNeighborhood,
        }
      : null;

    const comparisonMarkersList = comparisonNeighborhoods.map((n) => ({
      lat: n.lat,
      lng: n.lng,
      name: n.name,
      cheapestPrice: getCheapestPrice(n.name, n.price),
      data: n,
    }));

    return {
      markers: neighborhoodMarkers,
      selectedMarker,
      comparisonMarkers: comparisonMarkersList,
      isFilteredOut: false,
    };
  }, [maxPrice, minSafety, minComfort, selectedNeighborhood, comparisonNeighborhoods, isComparing, backgroundLayer]);

  return {
    searchQuery, setSearchQuery,
    checkin, setCheckin,
    checkout, setCheckout,
    guests, setGuests,
    hasSearched,
    selectedNeighborhood, setSelectedNeighborhood,
    maxPrice, setMaxPrice,
    minSafety, setMinSafety,
    minComfort, setMinComfort,
    minRating, setMinRating,
    backgroundLayer, setBackgroundLayer,
    isComparing, toggleComparisonMode,
    comparisonNeighborhoods, toggleNeighborhoodForComparison, removeFromComparison,
    selectedHotel, setSelectedHotel,
    handleSearch,
    filteredHotels,
    topRecommendations,
    mapData,
    isMapEmpty: mapData.isFilteredOut,
  };
}
