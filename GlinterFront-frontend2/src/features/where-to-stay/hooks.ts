import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import type { LoadState } from "@/shared/types/async-state";
import type { Hotel } from "./types";

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
  const [apiHotels, setApiHotels] = useState<Hotel[]>([]);
  const [staysState, setStaysState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    const city = searchParams.get("city");
    if (city) setSearchQuery(city);
  }, [searchParams]);

  useEffect(() => {
    staysApi.getStays({ pageSize: 100 })
      .then((response) => {
        setApiHotels(response.items.map(toHotel));
        setStaysState({ status: "ready" });
      })
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : "Could not load stays.";
        setStaysState({ status: "error", message });
        toast.error(message);
      });
  }, []);

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
    selectHotel,
    staysState,
    isLoadingStays: staysState.status === "loading",
    handleSearch: () => undefined,
    filteredHotels,
    mapData,
    isMapEmpty: staysState.status === "ready" && mapData.markers.length === 0,
  };
}
