import { useEffect, useState } from "react";
import { CalendarDays, Eye, Heart, LoaderCircle, MapPin, Pencil, Route, Trash2 } from "lucide-react";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { staysApi } from "@/shared/services/api-stays";
import { itinerariesApi } from "@/shared/services/api-itineraries";
import type { SavedItinerary } from "@/shared/types/itineraries";
import { formatUsdPrice } from "@/shared/lib/price";
import { toast } from "sonner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import HotelDetailsModal from "@/components/HotelDetailsModal";
import { toHotel } from "@/features/where-to-stay/hooks";
import type { Hotel } from "@/features/where-to-stay/types";
import { useNavigate } from "react-router-dom";

type FavoriteStay = Awaited<ReturnType<typeof staysApi.getFavorites>>["items"][number];

const SavedPage = () => {
  const navigate = useNavigate();
  const [tab, setTab] = useState<"trips" | "hotels">("trips");
  const [trips, setTrips] = useState<SavedItinerary[]>([]);
  const [hotels, setHotels] = useState<FavoriteStay[]>([]);
  const [loading, setLoading] = useState(true);
  const [deleteTrip, setDeleteTrip] = useState<SavedItinerary>();
  const [renamingId, setRenamingId] = useState<string>();
  const [renameValue, setRenameValue] = useState("");
  const [favoriteTotal, setFavoriteTotal] = useState(0);
  const [selectedHotel, setSelectedHotel] = useState<Hotel | null>(null);
  const [hotelDetailsLoading, setHotelDetailsLoading] = useState<number>();

  useEffect(() => {
    Promise.all([itinerariesApi.list(), staysApi.getAllFavorites()])
      .then(([savedTrips, savedHotels]) => {
        setTrips(savedTrips);
        setHotels(savedHotels.items);
        setFavoriteTotal(savedHotels.totalCount);
      })
      .catch((error: unknown) =>
        toast.error(error instanceof Error ? error.message : "Could not load saved items."))
      .finally(() => setLoading(false));
  }, []);

  const removeTrip = async (trip: SavedItinerary) => {
    try {
      await itinerariesApi.remove(trip.id);
      setTrips((current) => current.filter((item) => item.id !== trip.id));
      toast.success("Trip deleted.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not delete the trip.");
    }
  };

  const renameTrip = async (trip: SavedItinerary) => {
    const title = renameValue.trim();
    if (!title || title === trip.title) return;
    try {
      const updated = await itinerariesApi.update(trip.id, {
        title,
        destination: trip.destination,
        estimatedTotalCost: trip.estimatedTotalCost,
        currency: trip.currency,
        expectedUpdatedAtUtc: trip.updatedAtUtc,
      });
      setTrips((current) => current.map((item) => item.id === updated.id ? updated : item));
      setRenamingId(undefined);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not rename the trip.");
    }
  };

  const removeHotel = async (hotel: FavoriteStay) => {
    try {
      await staysApi.removeFavorite(hotel.id);
      setHotels((current) => current.filter((item) => item.id !== hotel.id));
      setFavoriteTotal((current) => Math.max(0, current - 1));
      toast.success(`${hotel.name} removed from favorites.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update favorites.");
    }
  };

  const openHotel = async (stayId: number) => {
    setHotelDetailsLoading(stayId);
    try {
      setSelectedHotel(toHotel(await staysApi.getStayById(stayId)));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "This hotel is no longer available.");
    } finally {
      setHotelDetailsLoading(undefined);
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-6xl px-4 py-8 sm:py-12">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-primary">Your saved travel</p>
        <h1 className="mt-2 text-3xl font-bold sm:text-4xl">My Trips & Hotels</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Open saved itineraries or manage hotels you want to revisit.
        </p>

        <div className="mt-6 inline-flex rounded-xl border border-border bg-card p-1">
          <button type="button" onClick={() => setTab("trips")} className={`rounded-lg px-4 py-2 text-sm ${tab === "trips" ? "bg-primary text-primary-foreground" : ""}`}>
            Trips ({trips.length})
          </button>
          <button type="button" onClick={() => setTab("hotels")} className={`rounded-lg px-4 py-2 text-sm ${tab === "hotels" ? "bg-primary text-primary-foreground" : ""}`}>
            Hotels ({favoriteTotal})
          </button>
        </div>

        {loading ? (
          <div role="status" className="mt-10 flex items-center gap-3 text-sm text-muted-foreground">
            <span className="h-5 w-5 animate-spin rounded-full border-2 border-primary/25 border-t-primary" />
            Loading saved travel…
          </div>
        ) : tab === "trips" ? (
          <section className="mt-6 grid gap-4">
            {trips.map((trip) => (
              <article key={trip.id} className="overflow-hidden rounded-2xl border border-border bg-card">
                <div className="flex flex-col justify-between gap-4 p-5 sm:flex-row sm:items-start">
                  <div className="min-w-0 flex-1">
                    {renamingId === trip.id ? (
                      <form className="flex max-w-md gap-2" onSubmit={(event) => {
                        event.preventDefault();
                        void renameTrip(trip);
                      }}>
                        <input autoFocus value={renameValue} maxLength={160} onChange={(event) => setRenameValue(event.target.value)} aria-label="Trip name" className="min-w-0 flex-1 rounded-lg border border-border bg-background px-3 py-2 text-sm" />
                        <button type="submit" className="rounded-lg bg-primary px-3 py-2 text-xs text-primary-foreground">Save</button>
                        <button type="button" onClick={() => setRenamingId(undefined)} className="rounded-lg border border-border px-3 py-2 text-xs">Cancel</button>
                      </form>
                    ) : <h2 className="text-lg font-bold">{trip.title}</h2>}
                    <p className="mt-1 flex items-center gap-2 text-sm text-muted-foreground">
                      <MapPin className="h-4 w-4" /> {trip.destination || "Destination not named"}
                    </p>
                    <p className="mt-2 flex items-center gap-2 text-xs text-muted-foreground">
                      <CalendarDays className="h-4 w-4" /> {trip.startDate} – {trip.endDate}
                      <span>· {trip.items.length} stops</span>
                      {trip.estimatedTotalCost != null && <span>· {formatUsdPrice(trip.estimatedTotalCost)}</span>}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <button type="button" onClick={() => navigate(`/itineraries/${trip.id}`)} className="rounded-lg border border-border px-3 py-2 text-xs">
                      <Route className="mr-1 inline h-3.5 w-3.5" /> Open editor
                    </button>
                    <button type="button" onClick={() => { setRenamingId(trip.id); setRenameValue(trip.title); }} aria-label={`Rename ${trip.title}`} className="rounded-lg border border-border p-2"><Pencil className="h-4 w-4" /></button>
                    <button type="button" onClick={() => setDeleteTrip(trip)} aria-label={`Delete ${trip.title}`} className="rounded-lg border border-destructive/30 p-2 text-destructive"><Trash2 className="h-4 w-4" /></button>
                  </div>
                </div>
              </article>
            ))}
            {trips.length === 0 && <Empty icon={Route} text="No saved trips yet. Build and save one in Where to Go." />}
          </section>
        ) : (
          <section className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {hotels.map((hotel) => (
              <article key={hotel.id} className="overflow-hidden rounded-2xl border border-border bg-card">
                <img src={hotel.primaryImage || "/placeholder.svg"} alt="" className="h-44 w-full object-cover" />
                <div className="p-4">
                  <h2 className="font-bold">{hotel.name}</h2>
                  <p className="mt-1 text-xs text-muted-foreground">{hotel.locationSummaryDescription || "Location details unavailable"}</p>
                  <div className="mt-4 flex items-center justify-between">
                    <span className="font-semibold">{hotel.price == null ? "Price unavailable" : `${formatUsdPrice(hotel.price)} / night`}</span>
                    <button type="button" onClick={() => void removeHotel(hotel)} aria-label={`Remove ${hotel.name} from favorites`} className="rounded-full bg-primary/10 p-2 text-primary"><Heart className="h-4 w-4 fill-current" /></button>
                  </div>
                  <button
                    type="button"
                    disabled={hotelDetailsLoading === hotel.id}
                    onClick={() => void openHotel(hotel.id)}
                    className="mt-3 flex min-h-10 w-full items-center justify-center gap-2 rounded-lg border border-border text-xs font-semibold disabled:opacity-60"
                  >
                    {hotelDetailsLoading === hotel.id
                      ? <LoaderCircle className="h-4 w-4 animate-spin" />
                      : <Eye className="h-4 w-4" />}
                    View details
                  </button>
                </div>
              </article>
            ))}
            {hotels.length === 0 && <Empty icon={Heart} text="No saved hotels yet." />}
          </section>
        )}
      </main>
      <ConfirmDialog
        open={Boolean(deleteTrip)}
        title="Delete saved trip?"
        description={deleteTrip ? `“${deleteTrip.title}” and its itinerary items will be permanently removed.` : ""}
        confirmLabel="Delete trip"
        destructive
        onOpenChange={(open) => { if (!open) setDeleteTrip(undefined); }}
        onConfirm={() => {
          if (deleteTrip) void removeTrip(deleteTrip);
          setDeleteTrip(undefined);
        }}
      />
      {selectedHotel && (
        <HotelDetailsModal
          hotel={selectedHotel}
          checkin=""
          checkout=""
          guests={1}
          onClose={() => setSelectedHotel(null)}
          onFavoriteChange={(stayId, isFavorite) => {
            if (!isFavorite) {
              setHotels((current) => current.filter((hotel) => hotel.id !== stayId));
              setFavoriteTotal((current) => Math.max(0, current - 1));
              setSelectedHotel(null);
            }
          }}
        />
      )}
      <Footer />
    </div>
  );
};

const Empty = ({ icon: Icon, text }: { icon: typeof Route; text: string }) => (
  <div className="col-span-full rounded-2xl border border-dashed border-border p-10 text-center text-muted-foreground">
    <Icon className="mx-auto mb-3 h-8 w-8" />
    <p>{text}</p>
  </div>
);

export default SavedPage;
