import { Star, MapPin, Wifi, Car, Coffee, Shield, X, Sparkles } from "lucide-react";
import hotelImg from "@/assets/hotel-1.jpg";
import { computePrice } from "@/features/where-to-stay/pricing";

interface HotelDetailsModalProps {
  hotel: {
    name: string;
    area: string;
    rating: number;
    reviews: number;
    price: number;
    comfort: number;
    safety: number;
    amenities: string[];
  } | null;
  checkin: string;
  checkout: string;
  onClose: () => void;
  onBook?: (hotel: NonNullable<HotelDetailsModalProps["hotel"]>) => void;
}

const HotelDetailsModal = ({ hotel, checkin, checkout, onClose, onBook }: HotelDetailsModalProps) => {
  if (!hotel) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose} />

      <div className="relative liquid-glass rounded-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto border-0 shadow-2xl">
        <button
          onClick={onClose}
          className="absolute top-3 right-3 z-10 w-8 h-8 rounded-full bg-white/10 flex items-center justify-center hover:bg-white/20 transition-colors"
        >
          <X className="w-4 h-4" />
        </button>

        <div className="relative h-48 overflow-hidden rounded-t-2xl">
          <img src={hotelImg} alt={hotel.name} className="w-full h-full object-cover" />
          <div className="absolute inset-0 bg-gradient-to-t from-[rgba(11,12,16,0.9)] to-transparent" />
          <div className="absolute bottom-3 left-4 bg-brand-purple/90 backdrop-blur-md px-3 py-1 rounded-full text-xs font-semibold flex items-center gap-1">
            <Sparkles className="w-3 h-3" /> {hotel.comfort}% Comfort
          </div>
        </div>

        <div className="p-5">
          <div className="flex items-start justify-between mb-4">
            <div>
              <h2 className="text-xl font-bold">{hotel.name}</h2>
              <p className="text-sm text-gray-400 flex items-center gap-1 mt-1">
                <MapPin className="w-3.5 h-3.5" /> {hotel.area}
              </p>
            </div>
            <div className="text-right">
              {(() => {
                const p = computePrice(hotel.price, checkin, checkout);
                if (p.nights > 0) {
                  return (
                    <>
                      <span className="text-lg font-bold text-brand-gold">${p.total}</span>
                      <p className="text-xs text-gray-400">total ({p.nights} nights)</p>
                    </>
                  );
                }
                return (
                  <>
                    <span className="text-lg font-bold text-brand-gold">${hotel.price}</span>
                    <p className="text-xs text-gray-400">per night</p>
                  </>
                );
              })()}
            </div>
          </div>

          <div className="flex items-center gap-3 mb-4">
            <span className="flex items-center gap-1 text-brand-gold">
              <Star className="w-4 h-4 fill-current" /> {hotel.rating}
            </span>
            <span className="text-sm text-gray-400">({hotel.reviews} reviews)</span>
          </div>

          {(() => {
            const p = computePrice(hotel.price, checkin, checkout);
            return (
              <div className="grid grid-cols-3 gap-3 mb-5">
                <div className="bg-white/5 rounded-xl p-3 text-center">
                  <Shield className="w-5 h-5 mx-auto mb-1 text-green-400" />
                  <p className="font-bold text-sm">{hotel.safety}%</p>
                  <p className="text-xs text-gray-400">Safety</p>
                </div>
                <div className="bg-white/5 rounded-xl p-3 text-center">
                  <span className="text-xl font-bold text-brand-gold block">
                    ${p.nights > 0 ? p.adjustedNightly : hotel.price}
                  </span>
                  <p className="text-xs text-gray-400">
                    {p.nights > 0 ? `Adj./Night` : "Price/Night"}
                  </p>
                </div>
                <div className="bg-white/5 rounded-xl p-3 text-center">
                  <span className="text-xl font-bold text-brand-teal block">{hotel.comfort}%</span>
                  <p className="text-xs text-gray-400">Comfort</p>
                </div>
              </div>
            );
          })()}

          <div className="mb-5">
            <h3 className="font-semibold text-sm mb-2">Amenities</h3>
            <div className="flex flex-wrap gap-2">
              {hotel.amenities.map((amenity) => (
                <span key={amenity} className="px-3 py-1.5 rounded-full bg-white/5 text-xs flex items-center gap-1.5">
                  {amenity === "wifi" && <Wifi className="w-3 h-3" />}
                  {amenity === "parking" && <Car className="w-3 h-3" />}
                  {amenity === "restaurant" && <Coffee className="w-3 h-3" />}
                  {amenity.charAt(0).toUpperCase() + amenity.slice(1)}
                </span>
              ))}
            </div>
          </div>

          {(() => {
            const p = computePrice(hotel.price, checkin, checkout);
            if (p.nights === 0) return null;
            return (
              <div className="mb-5 p-3 bg-white/5 rounded-xl text-xs space-y-1">
                <div className="flex justify-between">
                  <span className="text-gray-400">${p.adjustedNightly}/night × {p.nights} night{p.nights > 1 ? "s" : ""}</span>
                  <span>${p.subtotal}</span>
                </div>
                {p.seasonalMultiplier !== 1 && (
                  <div className="flex justify-between text-gray-400">
                    <span>Seasonal multiplier ({p.seasonalMultiplier}x)</span>
                    <span>{p.seasonalMultiplier > 1 ? "+" : ""}{Math.round((p.seasonalMultiplier - 1) * 100)}%</span>
                  </div>
                )}
                {p.discountPercent > 0 && (
                  <div className="flex justify-between text-green-400">
                    <span>Long-stay discount ({p.nights}+ nights)</span>
                    <span>-{p.discountPercent * 100}%</span>
                  </div>
                )}
                <div className="flex justify-between font-bold border-t border-white/10 pt-1 mt-1">
                  <span>Total</span>
                  <span className="text-brand-gold">${p.total}</span>
                </div>
              </div>
            );
          })()}

          <button
            onClick={() => onBook?.(hotel)}
            className="w-full py-3 rounded-xl bg-gradient-to-r from-brand-gold to-yellow-600 text-brand-dark font-semibold hover:opacity-90 transition"
          >
            Book Now
          </button>
        </div>
      </div>
    </div>
  );
};

export default HotelDetailsModal;
