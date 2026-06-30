import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import type { LoadState } from "@/shared/types/async-state";
import type { Hotel } from "./types";
import { regionsApi } from "@/shared/services/api-regions";
import type { RegionHierarchyGids } from "@/shared/types/regions";

const toHotel = (stay: Awaited<ReturnType<typeof staysApi.getStayById>>): Hotel => ({
  id: stay.id,
  name: stay.name,
  area: stay.locationSummaryDescription || "Egypt",
  rating: stay.rating ?? 0,
  reviews: stay.reviews ?? stay.featuredReviews.length,
  price: Math.round(stay.price ?? 0),
  amenities: stay.amenities,
  image: stay.images[0]?.link,
  description: stay.description,
  latitude: stay.latitude,
  longitude: stay.longitude,
});

export function useWhereToStay() {
  const [searchParams] = useSearchParams();
  const [searchQuery, setSearchQuery] = useState("");
  const [maxPrice, setMaxPrice] = useState(200);
  const [minRating, setMinRating] = useState(0);
  const [checkin, setCheckin] = useState("");
  const [checkout, setCheckout] = useState("");
  const [guests, setGuests] = useState(1);
  const [selectedHotel, setSelectedHotel] = useState<Hotel | null>(null);
  const [region, setRegion] = useState<RegionHierarchyGids>({});
  const [locatingRegion, setLocatingRegion] = useState(false);
  const [apiHotels, setApiHotels] = useState<Hotel[]>([]);
  const [staysState, setStaysState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    const city = searchParams.get("city");
    if (city) setSearchQuery(city);
  }, [searchParams]);

  useEffect(() => {
    let active = true;
    setStaysState({ status: "loading" });
    staysApi.getStays({
      pageSize: 100,
      adm0Gid: region.adm0Gid,
      adm1Gid: region.adm1Gid,
      adm2Gid: region.adm2Gid,
      adm3Gid: region.adm3Gid,
    })
      .then((response) => {
        if (!active) return;
        setApiHotels(response.items.map(toHotel));
        setStaysState({ status: "ready" });
      })
      .catch((error: unknown) => {
        if (!active) return;
        const message = error instanceof Error ? error.message : "Could not load stays.";
        setStaysState({ status: "error", message });
        toast.error(message);
      });
    return () => {
      active = false;
    };
  }, [region.adm0Gid, region.adm1Gid, region.adm2Gid, region.adm3Gid]);

  const filteredHotels = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return apiHotels
      .filter((hotel) =>
        (!query ||
          hotel.name.toLowerCase().includes(query) ||
          hotel.area.toLowerCase().includes(query)) &&
        hotel.price <= maxPrice &&
        hotel.rating >= minRating)
      .sort((left, right) => left.price - right.price);
  }, [apiHotels, maxPrice, minRating, searchQuery]);

  const selectHotel = async (hotel: Hotel) => {
    if (!hotel.id) return;
    try {
      setSelectedHotel(toHotel(await staysApi.getStayById(hotel.id)));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load stay details.");
    }
  };

  const resolveRegionByPoint = async (lat: number, lng: number) => {
    setLocatingRegion(true);
    try {
      setRegion(await regionsApi.getByPoint(lat, lng));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "No backend region contains this map point.");
    } finally {
      setLocatingRegion(false);
    }
  };

  const mapData = useMemo(() => ({
    markers: filteredHotels
      .filter((hotel) => hotel.latitude != null && hotel.longitude != null)
      .map((hotel) => ({
        lat: hotel.latitude as number,
        lng: hotel.longitude as number,
        name: hotel.name,
        cheapestPrice: hotel.price,
        data: hotel,
      })),
    selectedMarker: selectedHotel?.latitude != null && selectedHotel.longitude != null
      ? {
          lat: selectedHotel.latitude,
          lng: selectedHotel.longitude,
          name: selectedHotel.name,
          cheapestPrice: selectedHotel.price,
          data: selectedHotel,
        }
      : null,
    comparisonMarkers: [],
  }), [filteredHotels, selectedHotel]);

  return {
    searchQuery, setSearchQuery,
    checkin, setCheckin,
    checkout, setCheckout,
    guests, setGuests,
    maxPrice, setMaxPrice,
    minRating, setMinRating,
    selectedHotel, setSelectedHotel,
    region, setRegion,
    locatingRegion,
    resolveRegionByPoint,
    selectHotel,
    staysState,
    isLoadingStays: staysState.status === "loading",
    handleSearch: () => undefined,
    filteredHotels,
    mapData,
    isMapEmpty: staysState.status === "ready" && mapData.markers.length === 0,
  };
}
