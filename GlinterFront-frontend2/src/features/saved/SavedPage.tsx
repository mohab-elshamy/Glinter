import { useEffect, useState } from "react";
import { CalendarDays, Heart, MapPin, Pencil, Route, Trash2 } from "lucide-react";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { staysApi } from "@/shared/services/api-stays";
import { itinerariesApi } from "@/shared/services/api-itineraries";
import type { SavedItinerary } from "@/shared/types/itineraries";
import { formatUsdPrice } from "@/shared/lib/price";
import { toast } from "sonner";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";

type FavoriteStay = Awaited<ReturnType<typeof staysApi.getFavorites>>["items"][number];

const SavedPage = () => {
  const [tab, setTab] = useState<"trips" | "hotels">("trips");
  const [trips, setTrips] = useState<SavedItinerary[]>([]);
  const [hotels, setHotels] = useState<FavoriteStay[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<string>();
  const [deleteTrip, setDeleteTrip] = useState<SavedItinerary>();
  const [renamingId, setRenamingId] = useState<string>();
  const [renameValue, setRenameValue] = useState("");

  useEffect(() => {
    Promise.all([itinerariesApi.list(), staysApi.getFavorites()])
      .then(([savedTrips, savedHotels]) => {
        setTrips(savedTrips);
        setHotels(savedHotels.items);
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
      toast.success(`${hotel.name} removed from favorites.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update favorites.");
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
            Hotels ({hotels.length})
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
                    <button type="button" onClick={() => setExpandedId(expandedId === trip.id ? undefined : trip.id)} className="rounded-lg border border-border px-3 py-2 text-xs">
                      <Route className="mr-1 inline h-3.5 w-3.5" /> {expandedId === trip.id ? "Close" : "Open"}
                    </button>
                    <button type="button" onClick={() => { setRenamingId(trip.id); setRenameValue(trip.title); }} aria-label={`Rename ${trip.title}`} className="rounded-lg border border-border p-2"><Pencil className="h-4 w-4" /></button>
                    <button type="button" onClick={() => setDeleteTrip(trip)} aria-label={`Delete ${trip.title}`} className="rounded-lg border border-destructive/30 p-2 text-destructive"><Trash2 className="h-4 w-4" /></button>
                  </div>
                </div>
                {expandedId === trip.id && (
                  <div className="border-t border-border bg-secondary/20 p-5">
                    <div className="space-y-3">
                      {trip.items.map((item) => (
                        <div key={item.id} className="flex gap-3 rounded-xl bg-background p-3 text-sm">
                          <span className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-primary/15 text-xs font-bold text-primary">{item.sortOrder}</span>
                          <div>
                            <p className="font-semibold">{item.name}</p>
                            <p className="text-xs text-muted-foreground">Day {item.dayNumber}{item.startTime ? ` · ${item.startTime.slice(0, 5)}` : ""}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
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
