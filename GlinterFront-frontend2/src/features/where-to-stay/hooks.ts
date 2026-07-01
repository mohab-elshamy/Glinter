import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import type { LoadState } from "@/shared/types/async-state";
import type { Hotel } from "./types";
import { regionsApi } from "@/shared/services/api-regions";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import type {
  StayRegionGroupBy,
  StayRegionStatsDto,
  StayResponseDto,
} from "@/shared/types/api";

const toHotel = (stay: StayResponseDto): Hotel => ({
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
  const [appliedSearch, setAppliedSearch] = useState("");
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
  const [statsState, setStatsState] = useState<LoadState>({ status: "loading" });
  const [regionStats, setRegionStats] = useState<StayRegionStatsDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 12;

  useEffect(() => {
    const city = searchParams.get("city");
    if (city) {
      setSearchQuery(city);
      setAppliedSearch(city);
    }
  }, [searchParams]);

  useEffect(() => {
    let active = true;
    setStaysState({ status: "loading" });
    staysApi.getStays({
      page,
      pageSize,
      search: appliedSearch || undefined,
      adm0Gid: region.adm0Gid,
      adm1Gid: region.adm1Gid,
      adm2Gid: region.adm2Gid,
      adm3Gid: region.adm3Gid,
      maxPrice,
      minRating: minRating || undefined,
      sortBy: "Recommended",
      sortDirection: "Desc",
    })
      .then((response) => {
        if (!active) return;
        setApiHotels(response.items.map(toHotel));
        setTotalCount(response.totalCount);
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
  }, [
    appliedSearch,
    maxPrice,
    minRating,
    page,
    region.adm0Gid,
    region.adm1Gid,
    region.adm2Gid,
    region.adm3Gid,
  ]);

  useEffect(() => {
    let active = true;
    setStatsState({ status: "loading" });
    const groupBy: StayRegionGroupBy = region.adm3Gid
      ? "Adm3"
      : region.adm2Gid
        ? "Adm3"
        : region.adm1Gid
          ? "Adm2"
          : region.adm0Gid
            ? "Adm1"
            : "Adm0";
    staysApi.getRegionStats({
      groupBy,
      adm0Gid: region.adm0Gid,
      adm1Gid: region.adm1Gid,
      adm2Gid: region.adm2Gid,
      adm3Gid: region.adm3Gid,
      maxPrice,
      minRating: minRating || undefined,
    })
      .then((stats) => {
        if (!active) return;
        setRegionStats(stats);
        setStatsState({ status: "ready" });
      })
      .catch((error: unknown) => {
        if (!active) return;
        setRegionStats([]);
        setStatsState({
          status: "error",
          message: error instanceof Error ? error.message : "Could not load region statistics.",
        });
      });
    return () => {
      active = false;
    };
  }, [
    maxPrice,
    minRating,
    region.adm0Gid,
    region.adm1Gid,
    region.adm2Gid,
    region.adm3Gid,
  ]);

  const filteredHotels = apiHotels;

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
      setPage(1);
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
    maxPrice, setMaxPrice: (value: number) => { setMaxPrice(value); setPage(1); },
    minRating, setMinRating: (value: number) => { setMinRating(value); setPage(1); },
    selectedHotel, setSelectedHotel,
    region,
    setRegion: (value: RegionHierarchyGids) => { setRegion(value); setPage(1); },
    locatingRegion,
    resolveRegionByPoint,
    selectHotel,
    staysState,
    isLoadingStays: staysState.status === "loading",
    handleSearch: () => { setAppliedSearch(searchQuery.trim()); setPage(1); },
    filteredHotels,
    page,
    pageSize,
    totalCount,
    totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
    setPage,
    regionStats,
    statsState,
    mapData,
    isMapEmpty: staysState.status === "ready" && mapData.markers.length === 0,
  };
}
