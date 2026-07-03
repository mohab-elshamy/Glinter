import { useCallback, useEffect, useState } from "react";
import { Calendar, Clock3, Pencil } from "lucide-react";
import { toast } from "sonner";
import { authStorage } from "@/shared/lib/auth";
import { profilesApi } from "@/shared/services/api-profiles";
import type {
  BuddyAvailabilityDto,
  BuddyBookingDto,
  BuddyBookingStatus,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import { Dialog } from "@/components/ui/dialog";

const localInputToUtc = (value: string) => new Date(value).toISOString();

const MyBuddyScheduleTab = () => {
  const userId = authStorage.getUser()?.userId ?? "";
  const [slots, setSlots] = useState<BuddyAvailabilityDto[]>([]);
  const [bookings, setBookings] = useState<BuddyBookingDto[]>([]);
  const [startTime, setStartTime] = useState("");
  const [endTime, setEndTime] = useState("");
  const [price, setPrice] = useState(0);
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [editing, setEditing] = useState<BuddyAvailabilityDto>();
  const [editStart, setEditStart] = useState("");
  const [editEnd, setEditEnd] = useState("");
  const [editPrice, setEditPrice] = useState(0);
  const [editSaving, setEditSaving] = useState(false);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    try {
      const [loadedSlots, loadedBookings] = await Promise.all([
        profilesApi.getManagedBuddyAvailability(userId),
        profilesApi.getBuddyBookings(userId),
      ]);
      setSlots(loadedSlots);
      setBookings(loadedBookings);
      setState({ status: "ready" });
    } catch (error) {
      setState({
        status: "error",
        message: error instanceof Error ? error.message : "Could not load buddy schedule.",
      });
    }
  }, [userId]);

  useEffect(() => {
    void load();
  }, [load]);

  const createSlot = async () => {
    if (!startTime || !endTime) return;
    try {
      const created = await profilesApi.createBuddyAvailability(userId, {
        startTimeUtc: localInputToUtc(startTime),
        endTimeUtc: localInputToUtc(endTime),
        price,
      });
      setSlots((current) => [...current, created].sort(
        (left, right) => left.startTimeUtc.localeCompare(right.startTimeUtc)));
      setStartTime("");
      setEndTime("");
      toast.success("Availability published.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not publish availability.");
    }
  };

  const toggleSlot = async (slot: BuddyAvailabilityDto) => {
    try {
      const updated = await profilesApi.setBuddyAvailabilityActive(slot.id, !slot.isActive);
      setSlots((current) => current.map((item) => item.id === updated.id ? updated : item));
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update availability.");
    }
  };

  const toLocalInput = (value: string) => {
    const date = new Date(value);
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
    return local.toISOString().slice(0, 16);
  };

  const beginEdit = (slot: BuddyAvailabilityDto) => {
    setEditing(slot);
    setEditStart(toLocalInput(slot.startTimeUtc));
    setEditEnd(toLocalInput(slot.endTimeUtc));
    setEditPrice(slot.price);
  };

  const saveEdit = async () => {
    if (!editing || !editStart || !editEnd) return;
    if (new Date(editStart) >= new Date(editEnd)) {
      toast.error("End time must be after start time.");
      return;
    }
    setEditSaving(true);
    try {
      const updated = await profilesApi.updateBuddyAvailability(editing.id, {
        startTimeUtc: localInputToUtc(editStart),
        endTimeUtc: localInputToUtc(editEnd),
        price: editPrice,
      });
      setSlots((current) => current.map((slot) => slot.id === updated.id ? updated : slot));
      setEditing(undefined);
      toast.success("Availability updated.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update availability.");
    } finally {
      setEditSaving(false);
    }
  };

  const updateBooking = async (booking: BuddyBookingDto, status: BuddyBookingStatus) => {
    try {
      const updated = await profilesApi.updateBuddyBookingStatus(booking.id, status);
      setBookings((current) => current.map((item) => item.id === updated.id ? updated : item));
      toast.success(`Request ${status.toLowerCase()}.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update request.");
    }
  };

  if (state.status === "loading") {
    return <div className="card-glass p-10 text-center text-sm text-muted-foreground">Loading schedule and requests…</div>;
  }
  if (state.status === "error") {
    return (
      <div role="alert" className="rounded-xl border border-destructive/30 bg-destructive/10 p-5 text-sm">
        <p>{state.message}</p>
        <button onClick={() => void load()} className="mt-3 text-accent">Retry</button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <section className="card-glass p-6">
        <h2 className="mb-4 flex items-center gap-2 font-bold"><Calendar className="h-5 w-5" /> Publish availability</h2>
        <div className="grid gap-3 sm:grid-cols-3">
          <label className="text-xs">Starts<input aria-label="Availability start" type="datetime-local" value={startTime} onChange={(event) => setStartTime(event.target.value)} className="input-glass mt-1 w-full" /></label>
          <label className="text-xs">Ends<input aria-label="Availability end" type="datetime-local" value={endTime} onChange={(event) => setEndTime(event.target.value)} className="input-glass mt-1 w-full" /></label>
          <label className="text-xs">Price<input aria-label="Availability price" type="number" min="0" step="0.01" value={price} onChange={(event) => setPrice(Number(event.target.value))} className="input-glass mt-1 w-full" /></label>
        </div>
        <button disabled={!startTime || !endTime} onClick={() => void createSlot()} className="btn-accent mt-4 rounded-lg px-4 py-2 text-sm disabled:opacity-50">Publish slot</button>
      </section>

      <section className="card-glass p-6">
        <h2 className="mb-4 flex items-center gap-2 font-bold"><Clock3 className="h-5 w-5" /> My schedule</h2>
        {slots.length === 0 ? <p className="text-sm text-muted-foreground">No availability slots yet.</p> : (
          <div className="space-y-2">
            {slots.map((slot) => (
              <div key={slot.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-secondary/35 p-3 text-sm">
                <span>{new Date(slot.startTimeUtc).toLocaleString()} – {new Date(slot.endTimeUtc).toLocaleTimeString()} · ${slot.price}</span>
                <span className="flex items-center gap-3">
                  <small className={slot.isBooked ? "text-amber-300" : "text-muted-foreground"}>{slot.isBooked ? "Requested" : slot.isActive ? "Active" : "Inactive"}</small>
                  <button disabled={slot.isBooked} onClick={() => beginEdit(slot)} className="flex items-center gap-1 text-xs text-accent disabled:opacity-40">
                    <Pencil className="h-3 w-3" /> Edit
                  </button>
                  <button disabled={slot.isBooked} onClick={() => void toggleSlot(slot)} className="text-xs text-accent disabled:opacity-40">{slot.isActive ? "Deactivate" : "Activate"}</button>
                </span>
              </div>
            ))}
          </div>
        )}
      </section>

      <Dialog open={Boolean(editing)} onOpenChange={(open) => { if (!open) setEditing(undefined); }} title="Edit availability">
          <>
            <div className="mt-4 grid gap-3">
              <label className="text-xs">Starts<input type="datetime-local" value={editStart} onChange={(event) => setEditStart(event.target.value)} className="input-glass mt-1 w-full" /></label>
              <label className="text-xs">Ends<input type="datetime-local" value={editEnd} onChange={(event) => setEditEnd(event.target.value)} className="input-glass mt-1 w-full" /></label>
              <label className="text-xs">Price<input type="number" min="0" step="0.01" value={editPrice} onChange={(event) => setEditPrice(Number(event.target.value))} className="input-glass mt-1 w-full" /></label>
            </div>
            <div className="mt-5 flex justify-end gap-2">
              <button type="button" onClick={() => setEditing(undefined)} className="rounded-lg border border-border px-4 py-2 text-sm">Cancel</button>
              <button type="button" disabled={editSaving} onClick={() => void saveEdit()} className="btn-accent rounded-lg px-4 py-2 text-sm disabled:opacity-60">
                {editSaving ? "Saving…" : "Save changes"}
              </button>
            </div>
          </>
      </Dialog>

      <section className="card-glass p-6">
        <h2 className="mb-4 font-bold">Traveler requests</h2>
        {bookings.length === 0 ? <p className="text-sm text-muted-foreground">No buddy requests yet.</p> : (
          <div className="space-y-3">
            {bookings.map((booking) => (
              <article key={booking.id} className="rounded-xl bg-secondary/35 p-4 text-sm">
                <div className="flex flex-wrap justify-between gap-2">
                  <strong>{booking.travelerName}</strong><span>{booking.status}</span>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">{new Date(booking.startTimeUtc).toLocaleString()} · ${booking.totalPrice}</p>
                {booking.notes && <p className="mt-2 text-xs">{booking.notes}</p>}
                {booking.status === "Pending" && (
                  <div className="mt-3 flex gap-3">
                    <button onClick={() => void updateBooking(booking, "Accepted")} className="text-xs text-green-400">Accept</button>
                    <button onClick={() => void updateBooking(booking, "Rejected")} className="text-xs text-red-400">Reject</button>
                  </div>
                )}
                {booking.status === "Accepted" && (
                  <button onClick={() => void updateBooking(booking, "Completed")} className="mt-3 text-xs text-blue-400">Mark completed</button>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
};

export default MyBuddyScheduleTab;
