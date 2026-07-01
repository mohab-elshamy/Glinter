import { useMemo, useState } from "react";
import {
  Car,
  DollarSign,
  Gauge,
  Images,
  List,
  LocateFixed,
  Map,
  MapPin,
  ShieldCheck,
  Sparkles,
  Star,
  UtensilsCrossed,
  Wifi,
} from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import HotelDetailsModal from "@/components/HotelDetailsModal";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import SearchBar from "./components/SearchBar";
import HeatmapPanel from "./components/HeatmapPanel";
import FiltersPanel from "./components/FiltersPanel";
import RegionStatsPanel from "./components/RegionStatsPanel";
import { useWhereToStay } from "./hooks";
import { useStayMapSafety } from "./use-stay-map-safety";
import { detectedRegionLabel, useCurrentLocation } from "./use-current-location";
import { computePrice } from "./pricing";
import { formatUsdPrice } from "@/shared/lib/price";
import hotelImg from "@/assets/hotel-1.jpg";
import { toast } from "sonner";
import { authStorage } from "@/shared/lib/auth";
import type { Hotel } from "./types";
import { getComfortScore, type StayMapMode } from "./map-modes";
import { staysApi } from "@/shared/services/api-stays";
import type { SafetyPeriod } from "@/shared/types/safety";

const mapModes: Array<{
  value: StayMapMode;
  label: string;
  icon: typeof Map;
  description: string;
}> = [
  { value: "standard", label: "Standard", icon: Map, description: "Browse every mapped stay" },
  { value: "safety", label: "Safety", icon: ShieldCheck, description: "District safety signals" },
  { value: "comfort", label: "Comfort", icon: Gauge, description: "Ratings, reviews and amenities" },
  { value: "price", label: "Price", icon: DollarSign, description: "Compare with the regional average" },
];

const WhereToStayPage = () => {
  const {
    searchQuery, setSearchQuery,
    checkin, setCheckin,
    checkout, setCheckout,
    guests, setGuests,
    maxPrice, setMaxPrice,
    minRating, setMinRating,
    sortBy, sortDirection, setSorting,
    selectedHotel, setSelectedHotel,
    previewHotel, setPreviewHotel,
    selectHotel,
    region, setRegion,
    locatingRegion,
    resolveRegionByPoint,
    staysState,
    isLoadingStays,
    handleSearch,
    filteredHotels,
    page,
    pageSize,
    totalCount,
    totalPages,
    setPage,
    regionStats,
    statsState,
    isMapEmpty,
  } = useWhereToStay();

  const [viewMode, setViewMode] = useState<"map" | "list">("map");
  const [mapMode, setMapMode] = useState<StayMapMode>("standard");
  const [safetyPeriod, setSafetyPeriod] = useState<SafetyPeriod>("weekly");
  const [showFilters, setShowFilters] = useState(false);

  const safety = useStayMapSafety(
    region,
    viewMode === "map" && mapMode === "safety",
    safetyPeriod,
  );
  const currentLocation = useCurrentLocation();
  const currentRegionLabel = detectedRegionLabel(currentLocation.state.detectedRegion);

  const regionalSummary = useMemo(() => {
    const hotelsCount = regionStats.reduce((sum, item) => sum + item.hotelsCount, 0);
    const pricedRegions = regionStats.filter(
      (item) => item.averagePrice != null && item.hotelsCount > 0,
    );
    const weightedPriceTotal = pricedRegions.reduce(
      (sum, item) => sum + (item.averagePrice ?? 0) * item.hotelsCount,
      0,
    );
    const weightedHotelCount = pricedRegions.reduce(
      (sum, item) => sum + item.hotelsCount,
      0,
    );
    const visiblePrices = filteredHotels.flatMap((hotel) =>
      hotel.price == null ? [] : [hotel.price]);
    const fallbackAverage = visiblePrices.length > 0
      ? visiblePrices.reduce((sum, price) => sum + price, 0) / visiblePrices.length
      : undefined;
    const averageComfort = filteredHotels.length > 0
      ? Math.round(
          filteredHotels.reduce((sum, hotel) => sum + getComfortScore(hotel), 0) /
          filteredHotels.length,
        )
      : undefined;

    return {
      hotelsCount,
      averagePrice: weightedHotelCount > 0
        ? weightedPriceTotal / weightedHotelCount
        : fallbackAverage,
      averageComfort,
    };
  }, [filteredHotels, regionStats]);

  const handleBookHotel = async (hotel: Hotel) => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in as a traveler to book this stay.");
      return;
    }
    if (!hotel.id) {
      toast.error("This stay is not available for backend booking.");
      return;
    }
    if (!checkin || !checkout) {
      toast.error("Choose check-in and check-out dates first.");
      return;
    }
    if (hotel.price == null) {
      toast.error("This stay does not have a bookable backend price.");
      return;
    }

    const price = computePrice(hotel.price, checkin, checkout);
    if (price.nights < 1) {
      toast.error("Check-out must be after check-in.");
      return;
    }

    try {
      await staysApi.createBooking(hotel.id, {
        checkInDate: checkin,
        checkOutDate: checkout,
        guestCount: guests,
      });
      toast.success(`"${hotel.name}" booking requested.`);
      setSelectedHotel(null);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not create booking.");
    }
  };

  return (
    <div className="min-h-screen bg-[#0B0C10]">
      <Navbar />
      <main className="container mx-auto max-w-7xl px-4 pb-20 pt-6">
        <header className="mb-5 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-[0.22em] text-brand-gold">
              Find your base
            </p>
            <h1 className="text-3xl font-bold text-white sm:text-4xl">Where to stay</h1>
            <p className="mt-2 max-w-2xl text-sm text-gray-400">
              Compare live backend stays by location, comfort, regional price and safety context.
            </p>
          </div>
          <div className="flex rounded-xl border border-white/10 bg-white/5 p-1" aria-label="Results view">
            <button
              type="button"
              aria-pressed={viewMode === "map"}
              onClick={() => setViewMode("map")}
              className={`flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-semibold transition ${
                viewMode === "map" ? "bg-primary text-primary-foreground" : "text-gray-400 hover:text-white"
              }`}
            >
              <Map className="h-4 w-4" /> Map
            </button>
            <button
              type="button"
              aria-pressed={viewMode === "list"}
              onClick={() => setViewMode("list")}
              className={`flex items-center gap-2 rounded-lg px-4 py-2 text-xs font-semibold transition ${
                viewMode === "list" ? "bg-primary text-primary-foreground" : "text-gray-400 hover:text-white"
              }`}
            >
              <List className="h-4 w-4" /> List
            </button>
          </div>
        </header>

        <section className="card-glass mb-5 space-y-4 p-4">
          <SearchBar
            searchQuery={searchQuery}
            onSearchQueryChange={setSearchQuery}
            checkin={checkin}
            onCheckinChange={setCheckin}
            checkout={checkout}
            onCheckoutChange={setCheckout}
            guests={guests}
            onGuestsChange={setGuests}
            onSearch={handleSearch}
          />
          <div className="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
            <RegionCascadeSelect
              value={region}
              onChange={setRegion}
              label={locatingRegion ? "Finding region from map point…" : "Filter by backend region or click the map"}
              disabled={locatingRegion}
            />
            <div className="flex flex-wrap items-center gap-2">
              <label className="flex items-center gap-2 text-xs text-gray-400">
                <span>Sort</span>
                <select
                  value={`${sortBy}:${sortDirection}`}
                  onChange={(event) => {
                    const [nextSortBy, nextDirection] = event.target.value.split(":");
                    setSorting(nextSortBy as typeof sortBy, nextDirection as typeof sortDirection);
                  }}
                  className="rounded-lg border border-white/10 bg-[#15161c] px-3 py-2 text-xs text-white"
                >
                  <option value="Recommended:Desc">Recommended</option>
                  <option value="Price:Asc">Price: low to high</option>
                  <option value="Price:Desc">Price: high to low</option>
                  <option value="Rating:Desc">Highest rated</option>
                  <option value="Reviews:Desc">Most reviewed</option>
                  <option value="Name:Asc">Name: A–Z</option>
                  <option value="Name:Desc">Name: Z–A</option>
                  <option value="Newest:Desc">Newest first</option>
                  <option value="Newest:Asc">Oldest first</option>
                </select>
              </label>
              <button
                type="button"
                aria-expanded={showFilters}
                onClick={() => setShowFilters((value) => !value)}
                className="rounded-lg border border-white/10 px-3 py-2 text-xs text-gray-300 hover:bg-white/5"
              >
                {showFilters ? "Hide filters" : "Price & rating filters"}
              </button>
            </div>
          </div>
          {showFilters && (
            <div className="max-w-sm">
              <FiltersPanel
                maxPrice={maxPrice}
                onMaxPriceChange={setMaxPrice}
                minRating={minRating}
                onMinRatingChange={setMinRating}
              />
            </div>
          )}
        </section>

        <div className="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Metric label="Matching stays" value={isLoadingStays ? "…" : totalCount} />
          <Metric label="Mapped now" value={filteredHotels.filter((hotel) => hotel.latitude != null && hotel.longitude != null).length} />
          <Metric label="Regional average" value={formatUsdPrice(regionalSummary.averagePrice, "—")} />
          <Metric label="Average comfort" value={regionalSummary.averageComfort == null ? "—" : `${regionalSummary.averageComfort}/100`} />
        </div>

        {viewMode === "map" ? (
          <section>
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div className="flex max-w-full gap-2 overflow-x-auto pb-1">
                {mapModes.map((mode) => (
                  <button
                    key={mode.value}
                    type="button"
                    aria-pressed={mapMode === mode.value}
                    title={mode.description}
                    onClick={() => {
                      setMapMode(mode.value);
                      setPreviewHotel(null);
                    }}
                    className={`flex shrink-0 items-center gap-2 rounded-xl border px-3 py-2 text-xs font-semibold transition ${
                      mapMode === mode.value
                        ? "border-primary bg-primary/15 text-primary"
                        : "border-white/10 bg-white/5 text-gray-400 hover:text-white"
                    }`}
                  >
                    <mode.icon className="h-4 w-4" /> {mode.label}
                  </button>
                ))}
              </div>
              <div className="flex flex-wrap items-center gap-2">
                {mapMode === "safety" && (
                  <div className="flex rounded-lg border border-white/10 bg-white/5 p-1">
                    <button
                      type="button"
                      onClick={() => setSafetyPeriod("weekly")}
                      className={`rounded-md px-3 py-1.5 text-[11px] ${safetyPeriod === "weekly" ? "bg-white/10 text-white" : "text-gray-400"}`}
                    >
                      Current
                    </button>
                    <button
                      type="button"
                      onClick={() => setSafetyPeriod("historical")}
                      className={`rounded-md px-3 py-1.5 text-[11px] ${safetyPeriod === "historical" ? "bg-white/10 text-white" : "text-gray-400"}`}
                    >
                      Historical
                    </button>
                  </div>
                )}
                <button
                  type="button"
                  onClick={currentLocation.locate}
                  disabled={currentLocation.state.status === "locating"}
                  className="flex items-center gap-2 rounded-xl border border-sky-400/30 bg-sky-500/10 px-3 py-2 text-xs font-semibold text-sky-200 transition hover:bg-sky-500/20 disabled:cursor-wait disabled:opacity-60"
                >
                  <LocateFixed className={`h-4 w-4 ${currentLocation.state.status === "locating" ? "animate-pulse" : ""}`} />
                  {currentLocation.state.status === "locating" ? "Locating…" : "Locate me"}
                </button>
              </div>
            </div>

            {currentLocation.state.status === "ready" && currentLocation.state.position && (
              <div className="mb-3 flex flex-wrap items-center justify-between gap-2 rounded-xl border border-sky-500/25 bg-sky-500/10 p-3 text-xs text-sky-100">
                <span>
                  <strong>You are here</strong>
                  {" · "}accuracy ±{Math.round(currentLocation.state.position.accuracy)} m
                  {currentRegionLabel && <>{" · "}{currentRegionLabel}</>}
                </span>
                <span className="text-sky-200/70">
                  {currentLocation.state.regionMessage || "Region identified by the backend. Filters unchanged."}
                </span>
              </div>
            )}
            {["denied", "unavailable", "timeout", "insecure", "unsupported"].includes(currentLocation.state.status) && (
              <p role="alert" className="mb-3 rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-xs text-amber-100">
                {currentLocation.state.regionMessage}
              </p>
            )}

            {mapMode === "safety" && !region.adm0Gid && (
              <p className="mb-3 rounded-xl border border-amber-500/25 bg-amber-500/10 p-3 text-xs text-amber-100">
                Select a country and governorate to load district safety boundaries.
              </p>
            )}
            {mapMode === "safety" && region.adm0Gid && !region.adm1Gid && (
              <p className="mb-3 rounded-xl border border-blue-500/25 bg-blue-500/10 p-3 text-xs text-blue-100">
                Country safety scores are available. Select a governorate to draw the detailed heatmap.
              </p>
            )}
            {safety.state.status === "loading" && (
              <p className="mb-3 rounded-xl border border-white/10 bg-white/5 p-3 text-xs text-gray-300">
                Loading district safety scores and boundaries…
              </p>
            )}
            {safety.state.status === "error" && (
              <p role="alert" className="mb-3 rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-xs text-red-200">
                {safety.state.message}
              </p>
            )}
            {isMapEmpty && (
              <p className="mb-3 rounded-xl border border-white/10 bg-white/5 p-3 text-xs text-gray-300">
                No mapped stays match the synchronized search and filters.
              </p>
            )}

            <HeatmapPanel
              hotels={filteredHotels}
              selectedHotel={previewHotel}
              mode={mapMode}
              averagePrice={regionalSummary.averagePrice}
              safetyScores={safety.scoreByDistrict}
              safetyAreas={safety.overlayAreas}
              currentLocation={currentLocation.state.position}
              recenterSequence={currentLocation.state.requestSequence}
              onSelectHotel={setPreviewHotel}
              onClearHotel={() => setPreviewHotel(null)}
              onViewDetails={(hotel) => void selectHotel(hotel)}
              onMapClick={(lat, lng) => void resolveRegionByPoint(lat, lng)}
              height="min(72vh, 48rem)"
            />
          </section>
        ) : (
          <section>
            <RegionStatsPanel
              stats={regionStats}
              state={statsState}
              region={region}
              onSelectRegion={setRegion}
            />
            <div className="mb-7 mt-8 flex flex-wrap items-end justify-between gap-3">
              <div>
                <h2 className="text-2xl font-bold text-white">Stay results</h2>
                <p className="mt-1 text-sm text-gray-400">The list uses the same backend filters as the map.</p>
              </div>
              <span className="text-sm text-gray-500">
                {isLoadingStays
                  ? "Loading stays…"
                  : totalCount === 0
                    ? "0 hotels found"
                    : `${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, totalCount)} of ${totalCount}`}
              </span>
            </div>

            {staysState.status === "error" && (
              <div className="mb-8 rounded-xl border border-red-500/40 bg-red-500/10 p-4 text-sm text-red-300">
                {staysState.message}
              </div>
            )}
            {staysState.status === "ready" && filteredHotels.length === 0 && (
              <div className="card-glass mb-8 p-8 text-center text-sm text-gray-400">
                No stays match the current search and filters.
              </div>
            )}
            {filteredHotels.length > 0 && (
              <HotelGrid
                hotels={filteredHotels}
                checkin={checkin}
                checkout={checkout}
                onSelectHotel={(hotel) => void selectHotel(hotel)}
              />
            )}
            {staysState.status === "ready" && totalPages > 1 && (
              <nav aria-label="Stay results pages" className="mb-12 flex items-center justify-center gap-3">
                <button
                  disabled={page <= 1}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                  className="rounded-lg border border-border px-4 py-2 text-sm disabled:opacity-40"
                >
                  Previous
                </button>
                <span className="text-sm text-muted-foreground">Page {page} of {totalPages}</span>
                <button
                  disabled={page >= totalPages}
                  onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                  className="rounded-lg border border-border px-4 py-2 text-sm disabled:opacity-40"
                >
                  Next
                </button>
              </nav>
            )}
          </section>
        )}
      </main>

      <HotelDetailsModal
        hotel={selectedHotel}
        checkin={checkin}
        checkout={checkout}
        guests={guests}
        onClose={() => setSelectedHotel(null)}
        onBook={handleBookHotel}
      />
      <Footer />
    </div>
  );
};

const Metric = ({ label, value }: { label: string; value: string | number }) => (
  <div className="rounded-xl border border-white/10 bg-white/[0.04] p-3">
    <p className="text-[10px] uppercase tracking-wider text-gray-500">{label}</p>
    <p className="mt-1 text-lg font-bold text-white">{value}</p>
  </div>
);

const HotelGrid = ({
  hotels, checkin, checkout, onSelectHotel,
}: {
  hotels: Hotel[];
  checkin: string;
  checkout: string;
  onSelectHotel: (hotel: Hotel) => void;
}) => (
  <div className="mb-16 grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
    <AnimatePresence mode="popLayout">
      {hotels.map((hotel) => {
        const price = computePrice(hotel.price, checkin, checkout);
        const comfortScore = getComfortScore(hotel);
        const taxInclusivePrices = hotel.bookingPlatforms.flatMap((platform) =>
          platform.priceWithTax == null ? [] : [platform.priceWithTax]);
        const lowestTaxInclusivePrice = taxInclusivePrices.length > 0
          ? Math.min(...taxInclusivePrices)
          : undefined;
        return (
          <motion.article
            key={hotel.id ?? hotel.name}
            layout
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="liquid-glass group flex flex-col overflow-hidden rounded-2xl transition duration-300 hover:-translate-y-1"
          >
            <div className="relative h-52 shrink-0">
              <img src={hotel.image || hotelImg} alt={hotel.name} className="h-full w-full object-cover" />
              <div className="absolute left-3 top-3 flex items-center gap-1 rounded-full bg-black/75 px-3 py-1 text-xs font-semibold text-white backdrop-blur">
                <Sparkles className="h-3.5 w-3.5 text-cyan-300" /> {comfortScore} comfort
              </div>
              <div className="absolute right-3 top-3 rounded-full border border-brand-gold/30 bg-black/80 px-3 py-1 text-sm font-bold text-brand-gold backdrop-blur">
                {formatUsdPrice(hotel.price)}
                {hotel.price != null && hotel.price > 0 && <span className="text-[10px] font-normal text-gray-300">/night</span>}
              </div>
              {hotel.images.length > 1 && (
                <span className="absolute bottom-3 left-3 flex items-center gap-1 rounded-full bg-black/70 px-2 py-1 text-[10px] text-white backdrop-blur">
                  <Images className="h-3 w-3" /> {hotel.images.length}
                </span>
              )}
            </div>
            <div className="flex flex-1 flex-col p-5">
              <div className="mb-2 flex items-start justify-between gap-2">
                <h3 className="text-lg font-bold leading-tight text-white">{hotel.name}</h3>
                <span className="flex shrink-0 items-center gap-1 font-semibold text-brand-gold">
                  <Star className="h-4 w-4 fill-current" /> {hotel.rating}
                </span>
              </div>
              <p className="mb-5 flex items-center gap-1 text-sm text-gray-400">
                <MapPin className="h-3.5 w-3.5" /> {hotel.area} · {hotel.reviews} reviews
              </p>
              {hotel.description && (
                <p className="mb-4 line-clamp-2 text-xs leading-5 text-gray-400">{hotel.description}</p>
              )}
              <div className="mb-4 flex flex-wrap gap-1.5">
                {hotel.amenities.slice(0, 2).map((amenity) => (
                  <span key={amenity} className="rounded-full bg-white/5 px-2 py-1 text-[10px] text-gray-300">{amenity}</span>
                ))}
                {hotel.amenities.length > 2 && (
                  <span className="rounded-full bg-white/5 px-2 py-1 text-[10px] text-gray-400">+{hotel.amenities.length - 2}</span>
                )}
                {hotel.amenities.length === 0 && <span className="text-[10px] text-gray-500">Amenities not supplied</span>}
              </div>
              {lowestTaxInclusivePrice != null && (
                <p className="mb-3 text-[10px] text-gray-500">
                  Platform offer: <span className="text-gray-300">{formatUsdPrice(lowestTaxInclusivePrice)} including tax</span>
                </p>
              )}
              <div className="mt-auto flex items-center justify-between gap-3">
                <div className="flex gap-3 text-gray-400">
                  {hotel.amenities.some((item) => item.toLowerCase().includes("wifi")) && <Wifi className="h-4 w-4" />}
                  {hotel.amenities.some((item) => item.toLowerCase().includes("parking")) && <Car className="h-4 w-4" />}
                  {hotel.amenities.some((item) => item.toLowerCase().includes("restaurant")) && <UtensilsCrossed className="h-4 w-4" />}
                </div>
                <button
                  onClick={() => onSelectHotel(hotel)}
                  className="rounded-lg border border-primary/60 px-4 py-1.5 text-xs font-semibold text-primary transition hover:bg-primary/10"
                >
                  View details
                </button>
              </div>
              {price.nights > 0 && (
                <div className="mt-3 border-t border-white/5 pt-2 text-xs text-gray-500">
                  {price.total == null
                    ? "Price unavailable for this stay."
                    : <><strong className="text-brand-gold">{formatUsdPrice(price.total)}</strong> total for {price.nights} nights</>}
                </div>
              )}
            </div>
          </motion.article>
        );
      })}
    </AnimatePresence>
  </div>
);

export default WhereToStayPage;
