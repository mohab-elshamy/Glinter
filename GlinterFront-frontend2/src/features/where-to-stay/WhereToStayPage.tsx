import { useState } from "react";
import { Star, MapPin, Wifi, Car, Sparkles, UtensilsCrossed, SlidersHorizontal } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import HotelDetailsModal from "@/components/HotelDetailsModal";
import SearchBar from "./components/SearchBar";
import HeatmapPanel from "./components/HeatmapPanel";
import FiltersPanel from "./components/FiltersPanel";
import { useWhereToStay } from "./hooks";
import { computePrice } from "./pricing";
import hotelImg from "@/assets/hotel-1.jpg";
import { toast } from "sonner";
import { authStorage } from "@/shared/lib/auth";
import type { Hotel } from "./types";
import { staysApi } from "@/shared/services/api-stays";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import RegionStatsPanel from "./components/RegionStatsPanel";

const WhereToStayPage = () => {
  const {
    searchQuery, setSearchQuery,
    checkin, setCheckin,
    checkout, setCheckout,
    guests, setGuests,
    maxPrice, setMaxPrice,
    minRating, setMinRating,
    selectedHotel, setSelectedHotel,
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
    mapData,
    isMapEmpty,
  } = useWhereToStay();

  const [showFilters, setShowFilters] = useState(false);
  const [showMarkers, setShowMarkers] = useState(true);

  const handleBookHotel = async (hotel: typeof filteredHotels[number]) => {
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

    const p = computePrice(hotel.price, checkin, checkout);
    if (p.nights < 1) {
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
    <div className="min-h-screen" style={{ backgroundColor: '#0B0C10' }}>
      <Navbar />

      {/* HERO: map full-width, starts below navbar */}
      <section className="relative w-full" style={{ height: 'calc(100vh - 5rem)' }}>
        <div className="absolute inset-0 z-0">
          <HeatmapPanel
            mapData={mapData}
            onSelectHotel={(hotel) => void selectHotel(hotel)}
            onMapClick={(lat, lng) => void resolveRegionByPoint(lat, lng)}
            showMarkers={showMarkers}
            height="calc(100vh - 5rem)"
          />
        </div>

        {isMapEmpty && (
          <div className="absolute inset-0 z-10 flex items-center justify-center pointer-events-none">
            <div className="bg-black/70 backdrop-blur-sm rounded-2xl px-8 py-6 max-w-sm text-center border border-white/10">
              <p className="text-white/90 text-sm font-medium">
                No mapped stays match the selected backend region and filters.
              </p>
              <p className="text-white/50 text-xs mt-2">
                Try another region or price/rating filter. Some listings may not have coordinates yet.
              </p>
            </div>
          </div>
        )}

        <div
          className="absolute inset-0 z-[1] pointer-events-none"
          style={{
            background: 'radial-gradient(ellipse at center, transparent 30%, rgba(11,12,16,0.75) 100%)'
          }}
        />

        {/* Search bar — floats in map below navbar */}
        {showMarkers && (
          <div className="absolute top-3 left-1/2 -translate-x-1/2 max-w-4xl z-40 px-4" style={{ width: 'calc(100% - 2rem)' }}>
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
          </div>
        )}

        {/* FiltersPanel — floats top-right */}
        {showMarkers && (
          <>
            <div
              className="absolute right-8 z-40 hidden lg:block"
              style={{ top: '11rem', width: '18rem' }}
            >
              <FiltersPanel
                maxPrice={maxPrice}
                onMaxPriceChange={setMaxPrice}
                minRating={minRating}
                onMinRatingChange={setMinRating}
              />
            </div>
            {showFilters && (
              <div
                className="absolute right-4 z-40 lg:hidden"
                style={{ top: '7rem', width: '16rem' }}
              >
                <FiltersPanel
                  maxPrice={maxPrice}
                  onMaxPriceChange={setMaxPrice}
                  minRating={minRating}
                  onMinRatingChange={setMinRating}
                />
              </div>
            )}
          </>
        )}

        {/* Toggle markers */}
        <button
          onClick={() => setShowMarkers(!showMarkers)}
          className="absolute bottom-6 left-6 z-40 flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium bg-brand-dark/80 backdrop-blur-sm border border-brand-glassBorder hover:bg-brand-glass transition"
        >
          <span className="flex gap-0.5">
            <span className="w-2.5 h-2.5 rounded-full bg-green-400 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-yellow-400 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-red-400 inline-block" />
          </span>
          {showMarkers ? "Hide" : "Show"} Map UI
        </button>

        {/* Mobile filter toggle */}
        {showMarkers && (
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="lg:hidden absolute right-4 z-40 bottom-6 w-10 h-10 rounded-full bg-brand-dark/80 backdrop-blur-sm border border-brand-glassBorder flex items-center justify-center hover:bg-brand-glass transition"
          >
            <SlidersHorizontal className="w-4 h-4 text-white" />
          </button>
        )}
      </section>

      {/* MAIN CONTENT */}
      <main className="relative z-10 -mt-20 pb-20" style={{ backgroundColor: '#0B0C10' }}>
        <div className="container mx-auto max-w-7xl px-0 sm:px-2 max-md:px-4">
          <div className="card-glass mb-6 p-4">
            <RegionCascadeSelect
              value={region}
              onChange={setRegion}
              label={locatingRegion ? "Finding region from map point…" : "Filter by backend region or click the map"}
              disabled={locatingRegion}
            />
          </div>
          <RegionStatsPanel
            stats={regionStats}
            state={statsState}
            region={region}
            onSelectRegion={setRegion}
          />

          <div className="flex justify-between items-end mb-8 pt-10 max-md:flex-col max-md:items-start max-md:gap-2">
            <div>
              <h3 className="text-2xl font-bold mb-1">
                Search results{searchQuery
                  ? <> for <span className="text-brand-gold">'{searchQuery}'</span></>
                  : ''}
              </h3>
              <p className="text-gray-400 text-sm">Showing top matches based on your preferences</p>
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

        </div>
      </main>

      <HotelDetailsModal hotel={selectedHotel} checkin={checkin} checkout={checkout} onClose={() => setSelectedHotel(null)} onBook={handleBookHotel} />
      <Footer />
    </div>
  );
};

const HotelGrid = ({
  hotels, checkin, checkout, onSelectHotel,
}: {
  hotels: Hotel[];
  checkin: string;
  checkout: string;
  onSelectHotel: (h: Hotel) => void;
}) => {
  if (hotels.length === 0) return null;

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 max-md:gap-4 mb-16">
      <AnimatePresence mode="popLayout">
        {hotels.map((h) => {
          const p = computePrice(h.price, checkin, checkout);
          const matchPercent = Math.round((h.rating / 5) * 100);

          return (
            <motion.div
              key={h.name}
              layout
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              className="liquid-glass rounded-2xl overflow-hidden group hover:-translate-y-1 transition duration-300 flex flex-col"
            >
              <div className="relative h-56 shrink-0">
                <img src={h.image || hotelImg} alt={h.name} className="w-full h-full object-cover" />
                <div className={`absolute top-3 left-3 ${matchPercent >= 85 ? 'bg-[#0EA5E9]' : 'bg-[#8A2BE2]'} text-white px-3 py-1 rounded-full text-xs font-semibold flex items-center gap-1 shadow-lg`}>
                  <Sparkles className="w-3.5 h-3.5" /> {matchPercent}% Match
                </div>
                <div className="absolute top-3 right-3 bg-black/80 backdrop-blur-md px-3 py-1 rounded-full text-brand-gold text-sm font-bold border border-brand-gold/30 shadow-lg">
                  ${p.nights > 0 ? `${p.adjustedNightly}` : `${h.price}`}<span className="text-[10px] text-gray-300 font-normal">/night</span>
                </div>
              </div>

              <div className="p-5 flex flex-col flex-grow">
                <div>
                  <div className="flex justify-between items-start mb-2">
                    <h4 className="text-xl font-bold leading-tight">{h.name}</h4>
                    <div className="flex items-center gap-1 text-brand-gold font-semibold shrink-0">
                      <Star className="w-4 h-4 fill-current" /> <span className="text-base">{h.rating}</span>
                    </div>
                  </div>
                  <p className="text-sm text-gray-400 mb-5 flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5" /> {h.area} · <span className="text-gray-500">{h.reviews} reviews</span>
                  </p>
                </div>

                <div className="flex items-center justify-between mt-auto">
                  <div className="flex gap-4 text-gray-400">
                    {h.amenities.includes("wifi") && <span title="Free WiFi"><Wifi className="w-4 h-4" /></span>}
                    {h.amenities.includes("parking") && <span title="Parking"><Car className="w-4 h-4" /></span>}
                    {h.amenities.includes("restaurant") && <span title="Restaurant"><UtensilsCrossed className="w-4 h-4" /></span>}
                    {!h.amenities.includes("wifi") && !h.amenities.includes("parking") && !h.amenities.includes("restaurant") && (
                      <span className="text-xs text-gray-500">No amenities</span>
                    )}
                  </div>
                  <button
                    onClick={() => onSelectHotel(h)}
                    className="px-4 py-1.5 border border-[#8A2BE2]/60 text-[#8A2BE2] hover:bg-[#8A2BE2]/10 rounded-lg text-xs font-semibold transition shrink-0"
                  >
                    View Details
                  </button>
                </div>

                {p.nights > 0 && (
                  <div className="text-xs text-gray-500 mt-3 border-t border-white/5 pt-2">
                    <span className="text-brand-gold font-semibold">${p.total}</span> total · ${p.adjustedNightly}/night × {p.nights} night{p.nights > 1 ? "s" : ""}
                    {p.discountPercent > 0 && <span className="text-green-400 ml-1"> · -{p.discountPercent * 100}% off</span>}
                  </div>
                )}
              </div>
            </motion.div>
          );
        })}
      </AnimatePresence>
    </div>
  );
};

export default WhereToStayPage;
