import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Calendar, MapPin } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import { experiencesApi } from "@/shared/services/api-experiences";
import { formatUsdPrice } from "@/shared/lib/price";
import type { LoadState } from "@/shared/types/async-state";

interface BookingView {
  id: string;
  type: "stay" | "experience";
  name: string;
  startsAt: string;
  endsAt: string;
  totalPrice: number;
  status: "Pending" | "Confirmed" | "Completed" | "Cancelled";
}

const getStatusColor = (status: string) => {
  switch (status) {
    case "Confirmed": return "bg-green-500/20 text-green-400";
    case "Pending": return "bg-yellow-500/20 text-yellow-400";
    case "Completed": return "bg-blue-500/20 text-blue-400";
    case "Cancelled": return "bg-red-500/20 text-red-400";
    default: return "bg-gray-500/20 text-gray-400";
  }
};

const MyBookingsTab = () => {
  const navigate = useNavigate();
  const [bookings, setBookings] = useState<BookingView[]>([]);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });

  useEffect(() => {
    Promise.all([staysApi.getMyBookings(), experiencesApi.getMyBookings()])
      .then(([stayBookings, experienceBookings]) => {
        setBookings([
          ...stayBookings.map((booking): BookingView => ({
          id: booking.id,
          type: "stay",
          name: booking.stayName,
          startsAt: booking.checkInDate,
          endsAt: booking.checkOutDate,
          totalPrice: booking.totalPrice,
          status: booking.status,
        })),
        ...experienceBookings.map((booking): BookingView => ({
          id: booking.id,
          type: "experience",
          name: booking.experienceName,
          startsAt: booking.startTimeUtc,
          endsAt: booking.endTimeUtc,
          totalPrice: booking.totalPrice,
          status: booking.status,
          })),
        ]);
        setLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : "Could not load bookings.";
        setLoadState({ status: "error", message });
        toast.error(message);
      });
  }, []);

  const handleCancelBooking = async (booking: BookingView) => {
    try {
      if (booking.type === "stay") {
        await staysApi.cancelBooking(booking.id);
      } else {
        await experiencesApi.cancelBooking(booking.id);
      }
      setBookings(prev => prev.map(b => b.id === booking.id ? { ...b, status: "Cancelled" } : b));
      toast.info("Booking cancelled");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not cancel booking.");
    }
  };

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
      <div className="card-glass p-6">
        <h2 className="font-bold text-lg mb-4">My Bookings</h2>
        {loadState.status === "loading" ? (
          <p className="py-8 text-center text-sm text-muted-foreground">Loading bookings…</p>
        ) : loadState.status === "error" ? (
          <p className="rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">
            {loadState.message}
          </p>
        ) : bookings.length === 0 ? (
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
                    "bg-blue-500/20"
                  }`}>
                    <MapPin className="w-5 h-5 text-blue-500" />
                  </div>
                  <div>
                    <p className="font-semibold text-sm">{booking.name}</p>
                    <p className="text-xs text-muted-foreground flex items-center gap-1">
                      <Calendar className="w-3 h-3" /> {booking.startsAt} → {booking.endsAt}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="font-bold text-accent">{formatUsdPrice(booking.totalPrice)}</p>
                  <span className={`text-[10px] px-2 py-0.5 rounded-full ${getStatusColor(booking.status)}`}>
                    {booking.status.charAt(0).toUpperCase() + booking.status.slice(1)}
                  </span>
                </div>
                {booking.status !== "Cancelled" && booking.status !== "Completed" && (
                  <button
                    onClick={() => void handleCancelBooking(booking)}
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
