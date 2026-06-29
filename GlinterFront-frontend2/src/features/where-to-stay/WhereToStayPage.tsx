import { useState } from "react";
import { Search, Star, MapPin, Wifi, Car, Sparkles, Shield, DollarSign, Armchair, TrendingUp, X, UtensilsCrossed, SlidersHorizontal } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import HotelDetailsModal from "@/components/HotelDetailsModal";
import SearchBar from "./components/SearchBar";
import HeatmapPanel from "./components/HeatmapPanel";
import FiltersPanel from "./components/FiltersPanel";
import { useWhereToStay } from "./hooks";
import { allHotels } from "./data";
import { computePrice } from "./pricing";
import hotelImg from "@/assets/hotel-1.jpg";
import { toast } from "sonner";

const WhereToStayPage = () => {
  const {
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
    mapData,
    isMapEmpty,
  } = useWhereToStay();

  const [showFilters, setShowFilters] = useState(false);
  const [showMarkers, setShowMarkers] = useState(true);

  const handleBookHotel = (hotel: typeof filteredHotels[number]) => {
    const p = computePrice(hotel.price, checkin, checkout);
    const totalAmount = p.nights > 0 ? p.total : hotel.price;
    const bookingDate = checkin || new Date().toISOString().split("T")[0];
    const hotelId = hotel.name.toLowerCase().replace(/\s+/g, "-");

    const consumerBooking = {
      id: `booking-${Date.now()}`,
      type: "hotel" as const,
      name: hotel.name,
      date: bookingDate,
      status: "confirmed" as const,
      amount: totalAmount,
    };

    const ownerBooking = {
      id: `hotel-booking-${Date.now()}`,
      hotelId,
      hotelName: hotel.name,
      guestName: localStorage.getItem("profile_displayName") || "Guest",
      checkIn: checkin || bookingDate,
      checkOut: checkout || bookingDate,
      guestCount: guests,
      totalPrice: totalAmount,
      status: "pending" as const,
    };

    const existingConsumer = JSON.parse(localStorage.getItem("my_bookings") || "[]");
    existingConsumer.push(consumerBooking);
    localStorage.setItem("my_bookings", JSON.stringify(existingConsumer));

    const existingOwner = JSON.parse(localStorage.getItem("my_hotel_bookings") || "[]");
    existingOwner.push(ownerBooking);
    localStorage.setItem("my_hotel_bookings", JSON.stringify(existingOwner));

    toast.success(`"${hotel.name}" booked successfully! 🎉`);
  };

  return (
    <div className="min-h-screen" style={{ backgroundColor: '#0B0C10' }}>
      <Navbar />

      {/* HERO: map full-width, starts below navbar */}
      <section className="relative w-full" style={{ height: 'calc(100vh - 5rem)' }}>
        <div className="absolute inset-0 z-0">
          <HeatmapPanel
            mapData={mapData}
            backgroundLayer={backgroundLayer}
            selectedNeighborhood={selectedNeighborhood}
            isComparing={isComparing}
            comparisonNeighborhoods={comparisonNeighborhoods}
            filteredHotels={filteredHotels}
            onSelectNeighborhood={setSelectedNeighborhood}
            onToggleNeighborhoodForComparison={toggleNeighborhoodForComparison}
            showMarkers={showMarkers}
            height="calc(100vh - 5rem)"
          />
        </div>

        {isMapEmpty && (
          <div className="absolute inset-0 z-10 flex items-center justify-center pointer-events-none">
            <div className="bg-black/70 backdrop-blur-sm rounded-2xl px-8 py-6 max-w-sm text-center border border-white/10">
              <p className="text-white/90 text-sm font-medium">
                No places match all your filters.
              </p>
              <p className="text-white/50 text-xs mt-2">
                Try lowering your price, safety, or comfort requirements.
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
                minSafety={minSafety}
                onMinSafetyChange={setMinSafety}
                minComfort={minComfort}
                onMinComfortChange={setMinComfort}
                minRating={minRating}
                onMinRatingChange={setMinRating}
                backgroundLayer={backgroundLayer}
                onBackgroundLayerChange={setBackgroundLayer}
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
                  minSafety={minSafety}
                  onMinSafetyChange={setMinSafety}
                  minComfort={minComfort}
                  onMinComfortChange={setMinComfort}
                  minRating={minRating}
                  onMinRatingChange={setMinRating}
                  backgroundLayer={backgroundLayer}
                  onBackgroundLayerChange={setBackgroundLayer}
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

          <div className="flex justify-between items-end mb-8 pt-10 max-md:flex-col max-md:items-start max-md:gap-2">
            <div>
              <h3 className="text-2xl font-bold mb-1">
                Search results{searchQuery
                  ? <> for <span className="text-brand-gold">'{searchQuery}'</span></>
                  : ''}
              </h3>
              <p className="text-gray-400 text-sm">Showing top matches based on your preferences</p>
            </div>
            <span className="text-sm text-gray-500">{filteredHotels.length} hotels found</span>
          </div>

          {/* Comparison mode toggle & table */}
          <div className="mb-8">
            <button
              onClick={toggleComparisonMode}
              className={`px-6 py-2.5 rounded-xl border text-xs font-bold transition ${
                isComparing
                  ? "bg-brand-purple/20 border-brand-purple/50 text-brand-purple"
                  : "bg-brand-purple text-white border-brand-purple hover:bg-brand-purple/90"
              }`}
            >
              {isComparing ? "Exit Comparison Mode" : "Compare Neighborhoods"}
            </button>
            {isComparing && (
              <p className="text-xs text-gray-500 mt-2">
                Click neighborhoods on the map to add them to the comparison table below.
              </p>
            )}
            {isComparing && comparisonNeighborhoods.length > 0 && (
              <div className="mt-4">
                <ComparisonTable
                  neighborhoods={comparisonNeighborhoods}
                  onRemove={removeFromComparison}
                />
              </div>
            )}
          </div>

          {filteredHotels.length > 0 && (
            <HotelGrid
              hotels={filteredHotels}
              checkin={checkin}
              checkout={checkout}
              onSelectHotel={setSelectedHotel}
            />
          )}

          {filteredHotels.length === 0 && (
            <motion.div
              className="liquid-glass rounded-2xl p-10 text-center mb-10"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
            >
              <Search className="w-10 h-10 text-gray-400 mx-auto mb-3" />
              <p className="text-gray-400">No hotels match your filters. Try adjusting your search criteria.</p>
            </motion.div>
          )}

        </div>
      </main>

      <HotelDetailsModal hotel={selectedHotel} checkin={checkin} checkout={checkout} onClose={() => setSelectedHotel(null)} onBook={handleBookHotel} />
      <Footer />
    </div>
  );
};

const ComparisonTable = ({
  neighborhoods,
  onRemove,
}: {
  neighborhoods: { name: string; safety: number; price: number; comfort: number }[];
  onRemove: (name: string) => void;
}) => {
  const allHotelsLocal = allHotels;
  return (
    <div className="liquid-glass rounded-2xl p-3 md:p-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xl font-bold flex items-center gap-2">
          <TrendingUp className="w-5 h-5 text-brand-purple" />
          Neighborhood Comparison ({neighborhoods.length}/4)
        </h2>
        <span className="text-xs text-gray-400">Click neighborhoods on the map to add more</span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-white/10">
              <th className="text-left py-2 px-3 font-semibold text-gray-300">Neighborhood</th>
              <th className="text-center py-2 px-3 font-semibold text-gray-300"><Shield className="w-4 h-4 mx-auto mb-1 text-green-400" />Safety</th>
              <th className="text-center py-2 px-3 font-semibold text-gray-300"><DollarSign className="w-4 h-4 mx-auto mb-1 text-brand-gold" />Price/night</th>
              <th className="text-center py-2 px-3 font-semibold text-gray-300"><Armchair className="w-4 h-4 mx-auto mb-1 text-brand-teal" />Comfort</th>
              <th className="text-center py-2 px-3 font-semibold text-gray-300">Hotels</th>
              <th className="text-center py-2 px-3 font-semibold text-gray-300">Actions</th>
            </tr>
          </thead>
          <tbody>
            {neighborhoods.map((n) => {
              const hotelsInArea = allHotelsLocal.filter((h) => h.area === n.name);
              const prices = hotelsInArea.map(h => h.price);
              const minPrice = prices.length > 0 ? Math.min(...prices) : null;
              const maxPrice = prices.length > 0 ? Math.max(...prices) : null;
              return (
                <tr key={n.name} className="border-b border-white/5 hover:bg-white/5 transition-colors">
                  <td className="py-3 px-3 font-semibold">{n.name}</td>
                  <td className="text-center py-3 px-3">
                    <span className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${
                      n.safety >= 85 ? "bg-green-500/20 text-green-400" :
                      n.safety >= 70 ? "bg-brand-gold/20 text-brand-gold" :
                      "bg-red-500/20 text-red-400"
                    }`}>{n.safety}%</span>
                  </td>
                  <td className="text-center py-3 px-3">
                    <span className="text-xs font-medium text-brand-gold">
                      {minPrice !== null ? (minPrice === maxPrice ? `$${minPrice}` : `$${minPrice} - $${maxPrice}`) : "—"}
                    </span>
                  </td>
                  <td className="text-center py-3 px-3">
                    <span className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${
                      n.comfort >= 85 ? "bg-green-500/20 text-green-400" :
                      n.comfort >= 70 ? "bg-brand-gold/20 text-brand-gold" :
                      "bg-red-500/20 text-red-400"
                    }`}>{n.comfort}%</span>
                  </td>
                  <td className="text-center py-3 px-3 text-gray-400">{hotelsInArea.length}</td>
                  <td className="text-center py-3 px-3">
                    <button onClick={() => onRemove(n.name)} className="text-red-400 hover:text-red-300 transition-colors" title="Remove from comparison">
                      <X className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {neighborhoods.length < 4 && (
        <p className="text-xs text-gray-500 mt-3 text-center">
          Click up to {4 - neighborhoods.length} more neighborhood{4 - neighborhoods.length > 1 ? "s" : ""} on the map to compare
        </p>
      )}
    </div>
  );
};

const HotelGrid = ({
  hotels, checkin, checkout, onSelectHotel,
}: {
  hotels: { name: string; area: string; rating: number; reviews: number; price: number; comfort: number; safety: number; amenities: string[] }[];
  checkin: string;
  checkout: string;
  onSelectHotel: (h: { name: string; area: string; rating: number; reviews: number; price: number; comfort: number; safety: number; amenities: string[] }) => void;
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
                <img src={hotelImg} alt={h.name} className="w-full h-full object-cover" />
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
