import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Calendar, MapPin, Star } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import { authStorage } from "@/shared/lib/auth";

interface Booking {
  id: string;
  type: "hotel" | "experience";
  name: string;
  date: string;
  status: "pending" | "confirmed" | "completed" | "cancelled";
  amount: number;
}

const getStatusColor = (status: string) => {
  switch (status) {
    case "confirmed": return "bg-green-500/20 text-green-400";
    case "pending": return "bg-yellow-500/20 text-yellow-400";
    case "completed": return "bg-blue-500/20 text-blue-400";
    case "cancelled": return "bg-red-500/20 text-red-400";
    default: return "bg-gray-500/20 text-gray-400";
  }
};

const MyBookingsTab = () => {
  const navigate = useNavigate();
  const [bookings, setBookings] = useState<Booking[]>(() => {
    const saved = localStorage.getItem("my_bookings");
    return saved ? JSON.parse(saved) : [];
  });

  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;

    staysApi.getStays()
      .then(stays => {
        const stayNames = new Map(stays.map(s => [s.id, s.name]));
        return Promise.allSettled(
          stays.map(s => staysApi.getBookings(s.id))
        ).then(results => {
          const apiBookings: Booking[] = [];
          results.forEach((result, i) => {
            if (result.status === "fulfilled") {
              result.value.forEach(b => {
                apiBookings.push({
                  id: b.id,
                  type: "hotel",
                  name: stayNames.get(b.stayId) || "Hotel Stay",
                  date: b.checkInDate,
                  status: b.status as Booking["status"],
                  amount: b.totalPrice,
                });
              });
            }
          });
          if (apiBookings.length > 0) {
            setBookings(prev => {
              const existing = new Set(prev.map(b => b.id));
              const newOnes = apiBookings.filter(b => !existing.has(b.id));
              return [...newOnes, ...prev];
            });
          }
        });
      })
      .catch(() => {});
  }, []);

  const handleCancelBooking = async (bookingId: string) => {
    if (authStorage.isAuthenticated()) {
      try {
        await staysApi.cancelBooking(bookingId);
      } catch { }
    }
    setBookings(prev => prev.map(b =>
      b.id === bookingId ? { ...b, status: "cancelled" as const } : b
    ));
    toast.info("Booking cancelled");
  };

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
      <div className="card-glass p-6">
        <h2 className="font-bold text-lg mb-4">My Bookings</h2>
        {bookings.length === 0 ? (
          <div className="text-center py-8">
            <Calendar className="w-12 h-12 text-muted-foreground mx-auto mb-3 opacity-50" />
            <p className="text-muted-foreground text-sm">No bookings yet</p>
            <button
              onClick={() => navigate("/where-to-stay")}
              className="mt-3 text-accent text-sm hover:underline"
            >
              Browse hotels to book
            </button>
          </div>
        ) : (
          <div className="space-y-3">
            {bookings.map((booking) => (
              <div
                key={booking.id}
                className="flex items-center justify-between p-4 bg-secondary/30 rounded-lg"
              >
                <div className="flex items-center gap-3">
                  <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${
                    booking.type === "hotel" ? "bg-blue-500/20" : "bg-purple-500/20"
                  }`}>
                    {booking.type === "hotel" ? (
                      <MapPin className="w-5 h-5 text-blue-500" />
                    ) : (
                      <Star className="w-5 h-5 text-purple-500" />
                    )}
                  </div>
                  <div>
                    <p className="font-semibold text-sm">{booking.name}</p>
                    <p className="text-xs text-muted-foreground flex items-center gap-1">
                      <Calendar className="w-3 h-3" /> {booking.date}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="font-bold text-accent">${booking.amount}</p>
                  <span className={`text-[10px] px-2 py-0.5 rounded-full ${getStatusColor(booking.status)}`}>
                    {booking.status.charAt(0).toUpperCase() + booking.status.slice(1)}
                  </span>
                </div>
                {booking.status !== "cancelled" && booking.status !== "completed" && (
                  <button
                    onClick={() => handleCancelBooking(booking.id)}
                    className="text-xs text-red-500 hover:text-red-400 ml-2"
                  >
                    Cancel
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </motion.div>
  );
};

export default MyBookingsTab;
