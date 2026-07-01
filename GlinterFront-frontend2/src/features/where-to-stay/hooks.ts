import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import type { LoadState } from "@/shared/types/async-state";
import type { Hotel } from "./types";
import { regionsApi } from "@/shared/services/api-regions";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import type {
  SortDirection,
  StaySortBy,
  StayRegionGroupBy,
  StayRegionStatsDto,
  StayResponseDto,
} from "@/shared/types/api";

export const toHotel = (stay: StayResponseDto): Hotel => ({
  id: stay.id,
  name: stay.name,
  area: stay.locationSummaryDescription || "Egypt",
  rating: stay.rating ?? 0,
  reviews: stay.reviews ?? stay.featuredReviews.length,
  price: stay.price,
  amenities: stay.amenities,
  image: stay.images[0]?.link,
  images: stay.images,
  description: stay.description,
  bookingPlatforms: stay.bookingPlatforms,
  reviewsPerRating: stay.reviewsPerRating,
  featuredReviews: stay.featuredReviews,
  sourceType: stay.sourceType,
  website: stay.website,
  phoneInternational: stay.phoneInternational,
  googleMapsLink: stay.googleMapsLink,
  cid: stay.cid,
  createdAtUtc: stay.createdAtUtc,
  updatedAtUtc: stay.updatedAtUtc,
  latitude: stay.latitude,
  longitude: stay.longitude,
  adm0Gid: stay.adm0Gid,
  adm1Gid: stay.adm1Gid,
  adm2Gid: stay.adm2Gid,
  adm3Gid: stay.adm3Gid,
});

export function useWhereToStay() {
  const [searchParams] = useSearchParams();
  const [searchQuery, setSearchQuery] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [maxPrice, setMaxPrice] = useState<number>();
  const [minRating, setMinRating] = useState(0);
  const [sortBy, setSortBy] = useState<StaySortBy>("Recommended");
  const [sortDirection, setSortDirection] = useState<SortDirection>("Desc");
  const [checkin, setCheckin] = useState("");
  const [checkout, setCheckout] = useState("");
  const [guests, setGuests] = useState(1);
  const [selectedHotel, setSelectedHotel] = useState<Hotel | null>(null);
  const [previewHotel, setPreviewHotel] = useState<Hotel | null>(null);
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
      sortBy,
      sortDirection,
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
    sortBy,
    sortDirection,
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
    let detail: Hotel;
    try {
      detail = toHotel(await staysApi.getStayById(hotel.id));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load stay details.");
      return;
    }

    try {
      const names = await Promise.all([
        detail.adm0Gid
          ? regionsApi.getCountry(detail.adm0Gid).then((item) => item.nameEn)
          : undefined,
        detail.adm1Gid
          ? regionsApi.getGovernorate(detail.adm1Gid).then((item) => item.nameEn)
          : undefined,
        detail.adm2Gid
          ? regionsApi.getDistrict(detail.adm2Gid).then((item) => item.nameEn)
          : undefined,
        detail.adm3Gid
          ? regionsApi.getNeighbourhood(detail.adm3Gid).then((item) => item.nameEn ?? item.nameAr)
          : undefined,
      ]);
      setSelectedHotel({ ...detail, regionNames: names.filter((name): name is string => Boolean(name)) });
    } catch {
      setSelectedHotel(detail);
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
    selectedMarker: previewHotel?.latitude != null && previewHotel.longitude != null
      ? {
          lat: previewHotel.latitude,
          lng: previewHotel.longitude,
          name: previewHotel.name,
          cheapestPrice: previewHotel.price,
          data: previewHotel,
        }
      : null,
    comparisonMarkers: [],
  }), [filteredHotels, previewHotel]);

  return {
    searchQuery, setSearchQuery,
    checkin, setCheckin,
    checkout, setCheckout,
    guests, setGuests,
    maxPrice, setMaxPrice: (value?: number) => { setMaxPrice(value); setPage(1); },
    minRating, setMinRating: (value: number) => { setMinRating(value); setPage(1); },
    sortBy,
    sortDirection,
    setSorting: (nextSortBy: StaySortBy, nextDirection: SortDirection) => {
      setSortBy(nextSortBy);
      setSortDirection(nextDirection);
      setPage(1);
    },
    selectedHotel, setSelectedHotel,
    previewHotel, setPreviewHotel,
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
