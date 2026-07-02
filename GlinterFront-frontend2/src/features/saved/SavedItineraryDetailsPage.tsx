import { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowDown, ArrowUp, CalendarDays, LoaderCircle, Plus, Save, Trash2 } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import LeafletMap, { type MapMarker, type MapRouteLine } from "@/components/LeafletMap";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { itinerariesApi } from "@/shared/services/api-itineraries";
import { experiencesApi } from "@/shared/services/api-experiences";
import type { SavedItinerary, SavedItineraryItem, WeatherForecast } from "@/shared/types/itineraries";
import { toast } from "sonner";

const SavedItineraryDetailsPage = () => {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const [trip, setTrip] = useState<SavedItinerary>();
  const [draft, setDraft] = useState<SavedItineraryItem[]>([]);
  const [title, setTitle] = useState("");
  const [weather, setWeather] = useState<WeatherForecast>();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [conflict, setConflict] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [experienceId, setExperienceId] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setConflict(false);
    try {
      const loaded = await itinerariesApi.get(id);
      setTrip(loaded);
      setDraft(loaded.items);
      setTitle(loaded.title);
      const latitude = loaded.weatherLatitude ?? loaded.origin?.latitude ?? loaded.items[0]?.latitude;
      const longitude = loaded.weatherLongitude ?? loaded.origin?.longitude ?? loaded.items[0]?.longitude;
      if (latitude != null && longitude != null) {
        itinerariesApi.weather({
          latitude,
          longitude,
          location: loaded.weatherLocation || loaded.destination,
          startDate: loaded.startDate,
          endDate: loaded.endDate,
        }).then(setWeather).catch(() => setWeather(undefined));
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load this saved trip.");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { void load(); }, [load]);

  const normalizedItems = (items: SavedItineraryItem[]) =>
    [...items]
      .sort((left, right) => left.dayNumber - right.dayNumber || left.sortOrder - right.sortOrder)
      .map((item, index, all) => ({
        ...item,
        sortOrder: all.filter((candidate) => candidate.dayNumber === item.dayNumber)
          .findIndex((candidate) => candidate.id === item.id) + 1,
      }));

  const move = (item: SavedItineraryItem, direction: -1 | 1) => {
    setDraft((current) => {
      const day = current.filter((candidate) => candidate.dayNumber === item.dayNumber)
        .sort((left, right) => left.sortOrder - right.sortOrder);
      const index = day.findIndex((candidate) => candidate.id === item.id);
      const target = day[index + direction];
      if (!target) return current;
      return normalizedItems(current.map((candidate) =>
        candidate.id === item.id ? { ...candidate, sortOrder: target.sortOrder }
          : candidate.id === target.id ? { ...candidate, sortOrder: item.sortOrder }
            : candidate));
    });
  };

  const save = async () => {
    if (!trip || !title.trim()) return;
    setSaving(true);
    setConflict(false);
    try {
      const metadata = await itinerariesApi.update(trip.id, {
        title: title.trim(),
        destination: trip.destination,
        estimatedTotalCost: trip.estimatedTotalCost,
        currency: trip.currency,
        expectedUpdatedAtUtc: trip.updatedAtUtc,
      });
      const updated = await itinerariesApi.replaceItems(trip.id, {
        expectedUpdatedAtUtc: metadata.updatedAtUtc,
        items: normalizedItems(draft).map(({ id: itemId, ...item }) => ({ ...item, id: itemId })),
      });
      setTrip(updated);
      setDraft(updated.items);
      toast.success("Trip changes saved.");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Could not save the trip.";
      if (message.toLowerCase().includes("conflict") || message.toLowerCase().includes("changed")) {
        setConflict(true);
      }
      toast.error(message);
    } finally {
      setSaving(false);
    }
  };

  const addExperience = async () => {
    const numericId = Number(experienceId);
    if (!Number.isInteger(numericId) || numericId <= 0) return;
    try {
      const experience = await experiencesApi.getExperienceById(numericId);
      const dayNumber = 1;
      setDraft((current) => normalizedItems([...current, {
        id: crypto.randomUUID(),
        dayNumber,
        sortOrder: current.filter((item) => item.dayNumber === dayNumber).length + 1,
        entityType: "Experience",
        entityId: experience.id,
        name: experience.name,
        latitude: experience.latitude ?? 0,
        longitude: experience.longitude ?? 0,
        estimatedDurationMinutes: 120,
        estimatedCost: experience.startingPricePerPerson,
        category: experience.category,
        rating: experience.rating,
        imageUrl: experience.featuredImages[0]?.link,
      }]));
      setExperienceId("");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Experience was not found.");
    }
  };

  const markers = useMemo<MapMarker[]>(() => draft.map((item) => ({
    id: item.entityId,
    lat: item.latitude,
    lng: item.longitude,
    name: item.name,
    label: `${item.dayNumber}.${item.sortOrder}`,
  })), [draft]);
  const routeLines = useMemo<MapRouteLine[]>(() => draft.flatMap((item) => {
    const positions = (item.routeGeometryFromPrevious?.coordinates ?? []).flatMap((coordinate) =>
      coordinate.length >= 2 ? [[coordinate[1], coordinate[0]] as [number, number]] : []);
    return positions.length < 2 ? [] : [{
      id: item.id,
      positions,
      dashed: (item.routeWarningsFromPrevious?.length ?? 0) > 0,
      label: `${item.travelModeFromPrevious || "Travel"} · ${item.distanceKmFromPrevious ?? "?"} km`,
    }];
  }), [draft]);

  if (loading) return <div role="status" className="grid min-h-screen place-items-center"><LoaderCircle className="h-8 w-8 animate-spin" /></div>;
  if (!trip) return <div className="grid min-h-screen place-items-center">Saved trip not found.</div>;

  const dayCount = new Date(`${trip.endDate}T00:00:00`).getTime() -
    new Date(`${trip.startDate}T00:00:00`).getTime();
  const days = Array.from({ length: Math.floor(dayCount / 86_400_000) + 1 }, (_, index) => index + 1);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-6xl px-4 py-8">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
          <div className="min-w-0 flex-1">
            <input value={title} maxLength={160} onChange={(event) => setTitle(event.target.value)} aria-label="Trip title" className="w-full max-w-2xl rounded-xl border border-border bg-card px-4 py-3 text-2xl font-bold" />
            <p className="mt-2 text-sm text-muted-foreground">{trip.destination} · {trip.startDate} – {trip.endDate}</p>
          </div>
          <div className="flex gap-2">
            <button onClick={() => void save()} disabled={saving} className="btn-accent flex min-h-10 items-center gap-2 rounded-lg px-4 text-sm disabled:opacity-60"><Save className="h-4 w-4" /> {saving ? "Saving…" : "Save"}</button>
            <button onClick={() => { setTitle(trip.title); setDraft(trip.items); }} className="min-h-10 rounded-lg border border-border px-4 text-sm">Cancel changes</button>
            <button onClick={() => setConfirmDelete(true)} className="flex min-h-10 items-center gap-2 rounded-lg border border-destructive/30 px-4 text-sm text-destructive"><Trash2 className="h-4 w-4" /> Delete</button>
          </div>
        </div>

        {conflict && <div role="alert" className="mt-4 rounded-xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm">This trip changed elsewhere. <button onClick={() => void load()} className="font-semibold underline">Reload the latest version</button>.</div>}

        {(trip.plannerExplanation || trip.warnings.length > 0) && (
          <section className="mt-6 rounded-2xl border border-border bg-card p-5">
            <p>{trip.plannerExplanation}</p>
            {trip.warnings.map((warning) => <p key={warning} className="mt-2 text-xs text-amber-300">{warning}</p>)}
            <p className="mt-3 text-xs text-muted-foreground">{trip.pace} · {trip.travelMode} · {trip.totalDistanceKm?.toFixed(1)} km · {trip.totalTravelMinutes} min</p>
          </section>
        )}

        {markers.length > 0 && <section className="mt-6"><LeafletMap center={[markers[0].lat, markers[0].lng]} zoom={11} markers={markers} routeLines={routeLines} height="360px" /></section>}

        {weather && (
          <section className="mt-6 rounded-2xl border border-border bg-card p-5">
            <h2 className="font-bold">Weather</h2>
            {weather.isAvailable ? (
              <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                {weather.days.map((day) => <article key={day.date} className="rounded-xl bg-secondary p-3 text-xs"><strong>{day.date}</strong><p className="mt-2 text-lg">{day.temperatureMinC}°–{day.temperatureMaxC}°C</p><p>{day.condition}</p><p className="mt-1 text-muted-foreground">{day.precipitationProbabilityPercent}% rain · {day.windSpeedKph} km/h wind{day.humidityPercent == null ? "" : ` · ${day.humidityPercent}% humidity`}</p><p className="mt-2">{day.advice}</p></article>)}
              </div>
            ) : <p className="mt-2 text-sm text-muted-foreground">{weather.unavailableReason || "Forecast not yet available"}</p>}
          </section>
        )}

        <section className="mt-6 rounded-2xl border border-border bg-card p-5">
          <h2 className="font-bold">Add an experience</h2>
          <div className="mt-3 flex max-w-md gap-2"><input type="number" min="1" value={experienceId} onChange={(event) => setExperienceId(event.target.value)} placeholder="Experience ID" className="min-w-0 flex-1 rounded-lg border border-border bg-background px-3 py-2 text-sm" /><button onClick={() => void addExperience()} className="flex items-center gap-1 rounded-lg border border-border px-3 text-sm"><Plus className="h-4 w-4" /> Add</button></div>
        </section>

        <div className="mt-6 space-y-5">
          {days.map((day) => {
            const items = draft.filter((item) => item.dayNumber === day).sort((a, b) => a.sortOrder - b.sortOrder);
            return <section key={day} className="overflow-hidden rounded-2xl border border-border bg-card">
              <header className="border-b border-border p-4 font-bold"><CalendarDays className="mr-2 inline h-4 w-4" /> Day {day}</header>
              <div className="divide-y divide-border">
                {items.map((item, index) => <article key={item.id} className="grid gap-3 p-4 sm:grid-cols-[1fr_auto]">
                  <div>
                    <strong>{item.name}</strong>
                    <div className="mt-2 grid gap-2 sm:grid-cols-3">
                      <label className="text-xs">Start<input type="time" value={item.startTime?.slice(0, 5) ?? ""} onChange={(event) => setDraft((current) => current.map((candidate) => candidate.id === item.id ? { ...candidate, startTime: event.target.value } : candidate))} className="mt-1 w-full rounded border border-border bg-background p-2" /></label>
                      <label className="text-xs">Duration<input type="number" min="1" value={item.estimatedDurationMinutes ?? ""} onChange={(event) => setDraft((current) => current.map((candidate) => candidate.id === item.id ? { ...candidate, estimatedDurationMinutes: Number(event.target.value) } : candidate))} className="mt-1 w-full rounded border border-border bg-background p-2" /></label>
                      <label className="text-xs">Estimated cost<input type="number" min="0" step="0.01" value={item.estimatedCost ?? ""} onChange={(event) => setDraft((current) => current.map((candidate) => candidate.id === item.id ? { ...candidate, estimatedCost: Number(event.target.value) } : candidate))} className="mt-1 w-full rounded border border-border bg-background p-2" /></label>
                    </div>
                    {item.travelModeFromPrevious && <p className="mt-2 text-xs text-muted-foreground">{item.travelModeFromPrevious} · {item.routeProviderFromPrevious} · {item.distanceKmFromPrevious} km · {item.travelDurationMinutesFromPrevious} min</p>}
                  </div>
                  <div className="flex items-start gap-1">
                    <button disabled={index === 0} onClick={() => move(item, -1)} aria-label={`Move ${item.name} up`} className="rounded border border-border p-2 disabled:opacity-30"><ArrowUp className="h-4 w-4" /></button>
                    <button disabled={index === items.length - 1} onClick={() => move(item, 1)} aria-label={`Move ${item.name} down`} className="rounded border border-border p-2 disabled:opacity-30"><ArrowDown className="h-4 w-4" /></button>
                    <select aria-label={`Move ${item.name} to day`} value={item.dayNumber} onChange={(event) => setDraft((current) => normalizedItems(current.map((candidate) => candidate.id === item.id ? { ...candidate, dayNumber: Number(event.target.value), sortOrder: 999 } : candidate)))} className="rounded border border-border bg-background p-2 text-xs">{days.map((value) => <option key={value} value={value}>Day {value}</option>)}</select>
                    <button onClick={() => setDraft((current) => normalizedItems(current.filter((candidate) => candidate.id !== item.id)))} aria-label={`Remove ${item.name}`} className="rounded border border-destructive/30 p-2 text-destructive"><Trash2 className="h-4 w-4" /></button>
                  </div>
                </article>)}
                {items.length === 0 && <p className="p-4 text-sm text-muted-foreground">No stops on this day.</p>}
              </div>
            </section>;
          })}
        </div>
      </main>
      <ConfirmDialog open={confirmDelete} title="Delete saved trip?" description="This action cannot be undone." confirmLabel="Delete trip" destructive onOpenChange={setConfirmDelete} onConfirm={() => void itinerariesApi.remove(trip.id).then(() => navigate("/saved"))} />
      <Footer />
    </div>
  );
};

export default SavedItineraryDetailsPage;
