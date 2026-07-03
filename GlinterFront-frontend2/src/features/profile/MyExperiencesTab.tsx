import { useEffect, useState } from "react";
import { Calendar, Edit, Eye, EyeOff, ImagePlus, MapPin, Plus, Save, Sparkles, X } from "lucide-react";
import { toast } from "sonner";
import { experiencesApi } from "@/shared/services/api-experiences";
import { formatExperiencePrice, formatUsdPrice } from "@/shared/lib/price";
import type {
  CreateExperienceRequest,
  ExperienceAvailabilityDto,
  ExperienceBookingResponseDto,
  ExperienceBookingStatus,
  ExperienceCategory,
  ExperienceResponseDto,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import LeafletMap from "@/components/LeafletMap";
import { regionsApi } from "@/shared/services/api-regions";

const emptyExperience: CreateExperienceRequest = {
  category: "Historical",
  name: "",
  description: "",
  address: "",
  latitude: undefined,
  longitude: undefined,
  featuredImageLinks: [],
  hours: [],
  googleMapsLink: "",
  popularTimes: [],
  phoneInternational: "",
  priceRange: "",
  website: "",
  amenities: [],
};

const emptySlot = {
  startTimeUtc: "",
  endTimeUtc: "",
  capacity: 5,
  pricePerPerson: 0,
};

const message = (error: unknown) =>
  error instanceof Error ? error.message : "Something went wrong.";

const MyExperiencesTab = () => {
  const [experiences, setExperiences] = useState<ExperienceResponseDto[]>([]);
  const [categories, setCategories] = useState<ExperienceCategory[]>([]);
  const [slots, setSlots] = useState<Record<number, ExperienceAvailabilityDto[]>>({});
  const [bookings, setBookings] = useState<Record<number, ExperienceBookingResponseDto[]>>({});
  const [editing, setEditing] = useState<ExperienceResponseDto>();
  const [expandedId, setExpandedId] = useState<number>();
  const [slotExperienceId, setSlotExperienceId] = useState<number>();
  const [editingSlotId, setEditingSlotId] = useState<string>();
  const [form, setForm] = useState<CreateExperienceRequest>(emptyExperience);
  const [slotForm, setSlotForm] = useState(emptySlot);
  const [imageUrl, setImageUrl] = useState("");
  const [amenity, setAmenity] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [saving, setSaving] = useState(false);
  const [uploadingImages, setUploadingImages] = useState(false);
  const [locatingRegion, setLocatingRegion] = useState(false);

  useEffect(() => {
    Promise.all([experiencesApi.getMyExperiences(), experiencesApi.getCategories()])
      .then(([ownedExperiences, availableCategories]) => {
        setExperiences(ownedExperiences);
        setCategories(availableCategories);
        setLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        const errorText = message(error);
        setLoadState({ status: "error", message: errorText });
        toast.error(errorText);
      });
  }, []);

  const openCreate = () => {
    setEditing(undefined);
    setForm({ ...emptyExperience, featuredImageLinks: [], amenities: [], hours: [], popularTimes: [] });
    setShowForm(true);
  };

  const openEdit = (experience: ExperienceResponseDto) => {
    setEditing(experience);
    setForm({
      category: experience.category,
      name: experience.name,
      description: experience.description ?? "",
      address: experience.address ?? "",
      latitude: experience.latitude,
      longitude: experience.longitude,
      adm0Gid: experience.adm0Gid,
      adm1Gid: experience.adm1Gid,
      adm2Gid: experience.adm2Gid,
      adm3Gid: experience.adm3Gid,
      featuredImageLinks: experience.featuredImages.map((image) => image.link),
      hours: experience.hours.map(({ dayOfWeek, opensAt, closesAt }) => ({
        dayOfWeek,
        opensAt,
        closesAt,
      })),
      googleMapsLink: experience.googleMapsLink ?? "",
      popularTimes: experience.popularTimes.map(({ dayOfWeek, hourOfDay, popularityPercentage }) => ({
        dayOfWeek,
        hourOfDay,
        popularityPercentage,
      })),
      phoneInternational: experience.phoneInternational ?? "",
      priceRange: experience.priceRange ?? "",
      website: experience.website ?? "",
      amenities: experience.amenities,
    });
    setShowForm(true);
  };

  const saveExperience = async () => {
    if (!form.name.trim() || form.latitude == null || form.longitude == null) {
      toast.error("Experience name and a map location are required.");
      return;
    }

    setSaving(true);
    try {
      const saved = editing
        ? await experiencesApi.updateExperience(editing.id, form)
        : await experiencesApi.createExperience(form);
      setExperiences((current) =>
        editing
          ? current.map((experience) => experience.id === saved.id ? saved : experience)
          : [saved, ...current],
      );
      setShowForm(false);
      toast.success(editing ? "Experience updated." : "Experience created.");
    } catch (error) {
      toast.error(message(error));
    } finally {
      setSaving(false);
    }
  };

  const selectLocation = async (latitude: number, longitude: number) => {
    setForm((current) => ({ ...current, latitude, longitude }));
    setLocatingRegion(true);
    try {
      const hierarchy = await regionsApi.getByPoint(latitude, longitude);
      setForm((current) => ({ ...current, ...hierarchy, latitude, longitude }));
    } catch (error) {
      toast.error(message(error));
    } finally {
      setLocatingRegion(false);
    }
  };

  const uploadImages = async (files: FileList | null) => {
    if (!files?.length) return;
    setUploadingImages(true);
    try {
      const uploaded = await Promise.all(
        Array.from(files).map((file) => experiencesApi.uploadImage(file)),
      );
      setForm((current) => ({
        ...current,
        featuredImageLinks: [
          ...current.featuredImageLinks,
          ...uploaded.map((image) => image.link),
        ],
      }));
      toast.success(`${uploaded.length} image${uploaded.length === 1 ? "" : "s"} uploaded.`);
    } catch (error) {
      toast.error(message(error));
    } finally {
      setUploadingImages(false);
    }
  };

  const toggleActive = async (experience: ExperienceResponseDto) => {
    try {
      const updated = experience.isActive
        ? await experiencesApi.deactivateExperience(experience.id)
        : await experiencesApi.activateExperience(experience.id);
      setExperiences((current) =>
        current.map((item) => item.id === updated.id ? updated : item),
      );
    } catch (error) {
      toast.error(message(error));
    }
  };

  const loadManagement = async (experienceId: number) => {
    if (expandedId === experienceId) {
      setExpandedId(undefined);
      return;
    }
    setExpandedId(experienceId);
    try {
      const [loadedSlots, loadedBookings] = await Promise.all([
        experiencesApi.getManagedAvailability(experienceId),
        experiencesApi.getBookings(experienceId),
      ]);
      setSlots((current) => ({ ...current, [experienceId]: loadedSlots }));
      setBookings((current) => ({ ...current, [experienceId]: loadedBookings }));
    } catch (error) {
      toast.error(message(error));
    }
  };

  const createSlot = async () => {
    if (!slotExperienceId || !slotForm.startTimeUtc || !slotForm.endTimeUtc) {
      toast.error("Start and end times are required.");
      return;
    }
    try {
      const payload = {
        ...slotForm,
        startTimeUtc: new Date(slotForm.startTimeUtc).toISOString(),
        endTimeUtc: new Date(slotForm.endTimeUtc).toISOString(),
      };
      const saved = editingSlotId
        ? await experiencesApi.updateAvailability(editingSlotId, {
            ...payload,
            isActive: Object.values(slots)
              .flat()
              .find((slot) => slot.id === editingSlotId)?.isActive ?? true,
          })
        : await experiencesApi.createAvailability(slotExperienceId, payload);
      setSlots((current) => ({
        ...current,
        [slotExperienceId]: editingSlotId
          ? (current[slotExperienceId] ?? []).map((slot) => slot.id === saved.id ? saved : slot)
          : [...(current[slotExperienceId] ?? []), saved],
      }));
      setSlotExperienceId(undefined);
      setEditingSlotId(undefined);
      setSlotForm(emptySlot);
      toast.success(editingSlotId ? "Availability updated." : "Availability added.");
    } catch (error) {
      toast.error(message(error));
    }
  };

  const editSlot = (experienceId: number, slot: ExperienceAvailabilityDto) => {
    const toLocalInput = (value: string) => {
      const date = new Date(value);
      const offset = date.getTimezoneOffset() * 60_000;
      return new Date(date.getTime() - offset).toISOString().slice(0, 16);
    };
    setSlotExperienceId(experienceId);
    setEditingSlotId(slot.id);
    setSlotForm({
      startTimeUtc: toLocalInput(slot.startTimeUtc),
      endTimeUtc: toLocalInput(slot.endTimeUtc),
      capacity: slot.capacity,
      pricePerPerson: slot.pricePerPerson,
    });
  };

  const toggleSlot = async (experienceId: number, slot: ExperienceAvailabilityDto) => {
    try {
      const updated = slot.isActive
        ? await experiencesApi.deactivateAvailability(slot.id)
        : await experiencesApi.activateAvailability(slot.id);
      setSlots((current) => ({
        ...current,
        [experienceId]: (current[experienceId] ?? []).map((item) =>
          item.id === updated.id ? updated : item),
      }));
    } catch (error) {
      toast.error(message(error));
    }
  };

  const updateBooking = async (
    experienceId: number,
    bookingId: string,
    status: ExperienceBookingStatus,
  ) => {
    try {
      const updated = status === "Cancelled"
        ? await experiencesApi.cancelBooking(bookingId)
        : await experiencesApi.updateBookingStatus(bookingId, status);
      setBookings((current) => ({
        ...current,
        [experienceId]: (current[experienceId] ?? []).map((item) =>
          item.id === updated.id ? updated : item),
      }));
    } catch (error) {
      toast.error(message(error));
    }
  };

  if (loadState.status === "loading") {
    return <p className="text-sm text-muted-foreground">Loading your experiences…</p>;
  }
  if (loadState.status === "error") {
    return <p className="rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">{loadState.message}</p>;
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Sparkles className="h-6 w-6 text-accent" />
          <div>
            <h1 className="text-2xl font-bold">My Experiences</h1>
            <p className="text-xs text-muted-foreground">{experiences.length} backend listing(s)</p>
          </div>
        </div>
        <button onClick={openCreate} className="btn-accent flex items-center gap-2 rounded-lg px-4 py-2 text-sm">
          <Plus className="h-4 w-4" /> Add Experience
        </button>
      </div>

      {experiences.length === 0 ? (
        <div className="card-glass p-12 text-center">
          <Sparkles className="mx-auto mb-4 h-16 w-16 opacity-30" />
          <p className="mb-4 text-sm text-muted-foreground">No provider experiences yet.</p>
          <button onClick={openCreate} className="btn-accent rounded-lg px-5 py-2 text-sm">Add your first experience</button>
        </div>
      ) : (
        <div className="space-y-4">
          {experiences.map((experience) => (
            <section key={experience.id} className="card-glass p-5">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <div className="mb-2 flex items-center gap-2">
                    <h3 className="text-lg font-bold">{experience.name}</h3>
                    <span className="rounded-full bg-accent/20 px-2 py-0.5 text-[10px] text-accent">{experience.category}</span>
                    <span className={`rounded-full px-2 py-0.5 text-[10px] ${
                      experience.moderationStatus === "Approved"
                        ? "bg-green-500/20 text-green-400"
                        : experience.moderationStatus === "Pending"
                          ? "bg-yellow-500/20 text-yellow-400"
                          : "bg-red-500/20 text-red-400"
                    }`}>
                      {experience.moderationStatus}
                    </span>
                    <span className={`rounded-full px-2 py-0.5 text-[10px] ${experience.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"}`}>
                      {experience.isActive ? "Active" : "Inactive"}
                    </span>
                  </div>
                  <p className="flex items-center gap-1 text-xs text-muted-foreground"><MapPin className="h-3 w-3" /> {experience.address || "No address"}</p>
                  <p className="mt-2 text-sm text-accent">
                    {formatExperiencePrice(experience.startingPricePerPerson, experience.priceRange)}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-1">{experience.amenities.map((item) => <span key={item} className="rounded-full bg-secondary px-2 py-1 text-[10px]">{item}</span>)}</div>
                </div>
                <div className="flex gap-1">
                  <button onClick={() => openEdit(experience)} className="rounded-lg p-2 hover:bg-secondary"><Edit className="h-4 w-4" /></button>
                  <button onClick={() => void toggleActive(experience)} className="rounded-lg p-2 hover:bg-secondary">{experience.isActive ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}</button>
                </div>
              </div>

              <button onClick={() => void loadManagement(experience.id)} className="mt-4 text-xs text-accent hover:underline">
                {expandedId === experience.id ? "Hide" : "Manage"} availability and bookings
              </button>

              {expandedId === experience.id && (
                <div className="mt-4 grid gap-5 border-t border-border pt-4 md:grid-cols-2">
                  <div>
                    <div className="mb-2 flex items-center justify-between">
                      <h4 className="text-sm font-semibold">Availability</h4>
                      <button onClick={() => { setEditingSlotId(undefined); setSlotForm(emptySlot); setSlotExperienceId(experience.id); }} className="text-xs text-accent">+ Add slot</button>
                    </div>
                    <div className="space-y-2">
                      {(slots[experience.id] ?? []).length === 0 && <p className="text-xs text-muted-foreground">No availability slots.</p>}
                      {(slots[experience.id] ?? []).map((slot) => (
                        <div key={slot.id} className="rounded-lg bg-secondary/30 p-3 text-xs">
                          <div className="flex items-center justify-between">
                            <span>{new Date(slot.startTimeUtc).toLocaleString()}</span>
                            <button onClick={() => void toggleSlot(experience.id, slot)} className={slot.isActive ? "text-green-400" : "text-red-400"}>{slot.isActive ? "Active" : "Inactive"}</button>
                          </div>
                          <p className="mt-1 text-muted-foreground">{slot.remainingCapacity}/{slot.capacity} remaining · {formatUsdPrice(slot.pricePerPerson)}/person</p>
                          <button onClick={() => editSlot(experience.id, slot)} className="mt-1 text-accent">Edit</button>
                        </div>
                      ))}
                    </div>
                  </div>
                  <div>
                    <h4 className="mb-2 text-sm font-semibold">Bookings</h4>
                    <div className="space-y-2">
                      {(bookings[experience.id] ?? []).length === 0 && <p className="text-xs text-muted-foreground">No bookings.</p>}
                      {(bookings[experience.id] ?? []).map((booking) => (
                        <div key={booking.id} className="rounded-lg bg-secondary/30 p-3 text-xs">
                          <div className="flex items-center justify-between">
                            <span>{booking.travelerName} · {booking.guestsCount} guest(s)</span>
                            <span>{booking.status}</span>
                          </div>
                          <p className="mt-1 text-muted-foreground">{new Date(booking.startTimeUtc).toLocaleString()} · {formatUsdPrice(booking.totalPrice)}</p>
                          {booking.status === "Pending" && (
                            <div className="mt-2 flex gap-3">
                              <button onClick={() => void updateBooking(experience.id, booking.id, "Confirmed")} className="text-green-400">Confirm</button>
                              <button onClick={() => void updateBooking(experience.id, booking.id, "Cancelled")} className="text-red-400">Cancel</button>
                            </div>
                          )}
                          {booking.status === "Confirmed" && <button onClick={() => void updateBooking(experience.id, booking.id, "Completed")} className="mt-2 text-blue-400">Complete</button>}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </section>
          ))}
        </div>
      )}

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl border border-border bg-background p-6">
            <div className="mb-5 flex items-center justify-between"><h2 className="text-xl font-bold">{editing ? "Edit experience" : "Create experience"}</h2><button onClick={() => setShowForm(false)}><X className="h-5 w-5" /></button></div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="text-xs">Name *<input className="input-glass mt-1 w-full" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
              <label className="text-xs">Category<select className="input-glass mt-1 w-full" value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value as ExperienceCategory })}>{(categories.length ? categories : ["Historical", "Nature", "Shopping", "Nightlife", "Dining"]).map((category) => <option key={category}>{category}</option>)}</select></label>
              <label className="text-xs md:col-span-2">Description<textarea className="input-glass mt-1 w-full" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
              <div className="md:col-span-2">
                <RegionCascadeSelect
                  value={form}
                  onChange={(selection) => setForm({ ...form, ...selection })}
                  label="Backend region"
                />
              </div>
              <label className="text-xs md:col-span-2">Address<input className="input-glass mt-1 w-full" value={form.address} onChange={(event) => setForm({ ...form, address: event.target.value })} /></label>
              <div className="md:col-span-2">
                <p className="mb-2 text-xs">
                  Click the map to set the exact location and resolve its backend region.
                  {locatingRegion && <span className="ml-2 text-accent">Resolving region…</span>}
                </p>
                <LeafletMap
                  center={[
                    form.latitude ?? 30.0444,
                    form.longitude ?? 31.2357,
                  ]}
                  zoom={form.latitude == null ? 10 : 14}
                  markers={form.latitude != null && form.longitude != null
                    ? [{
                        lat: form.latitude,
                        lng: form.longitude,
                        name: form.name || "Selected experience location",
                      }]
                    : []}
                  onMapClick={(latitude, longitude) => void selectLocation(latitude, longitude)}
                  showSearch
                  showFullscreen={false}
                  showLegend={false}
                  height="280px"
                />
                <p className="mt-2 text-[11px] text-muted-foreground">
                  {form.latitude == null
                    ? "No location selected."
                    : `${form.latitude.toFixed(6)}, ${form.longitude?.toFixed(6)}`}
                </p>
              </div>
              <label className="text-xs">Price range<input className="input-glass mt-1 w-full" placeholder="$20–$50" value={form.priceRange} onChange={(event) => setForm({ ...form, priceRange: event.target.value })} /></label>
              <label className="text-xs">Website<input className="input-glass mt-1 w-full" value={form.website} onChange={(event) => setForm({ ...form, website: event.target.value })} /></label>
              <label className="text-xs">Phone<input className="input-glass mt-1 w-full" value={form.phoneInternational} onChange={(event) => setForm({ ...form, phoneInternational: event.target.value })} /></label>
              <label className="text-xs">Google Maps link<input className="input-glass mt-1 w-full" value={form.googleMapsLink} onChange={(event) => setForm({ ...form, googleMapsLink: event.target.value })} /></label>
            </div>
            <div className="mt-5">
              <p className="mb-2 text-xs">Images</p>
              <label className="mb-3 flex cursor-pointer items-center justify-center gap-2 rounded-xl border border-dashed border-accent/50 bg-accent/5 p-4 text-sm text-accent">
                <ImagePlus className="h-4 w-4" />
                {uploadingImages ? "Uploading images…" : "Upload JPEG, PNG, or WebP images"}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  multiple
                  disabled={uploadingImages}
                  className="sr-only"
                  onChange={(event) => void uploadImages(event.target.files)}
                />
              </label>
              <div className="flex gap-2">
                <input className="input-glass flex-1" value={imageUrl} onChange={(event) => setImageUrl(event.target.value)} placeholder="Or add an external image URL" />
                <button onClick={() => { if (imageUrl.trim()) setForm({ ...form, featuredImageLinks: [...form.featuredImageLinks, imageUrl.trim()] }); setImageUrl(""); }} className="rounded bg-secondary px-3">Add</button>
              </div>
              <div className="mt-3 grid grid-cols-3 gap-2 sm:grid-cols-4">
                {form.featuredImageLinks.map((url) => (
                  <button
                    key={url}
                    onClick={() => setForm({ ...form, featuredImageLinks: form.featuredImageLinks.filter((item) => item !== url) })}
                    className="group relative overflow-hidden rounded-lg border border-border"
                    title="Click to remove"
                  >
                    <img src={url} alt="" className="h-20 w-full object-cover" />
                    <span className="absolute inset-0 hidden items-center justify-center bg-black/60 text-xs group-hover:flex">Remove</span>
                  </button>
                ))}
              </div>
            </div>
            <div className="mt-5"><p className="mb-2 text-xs">Amenities</p><div className="flex gap-2"><input className="input-glass flex-1" value={amenity} onChange={(event) => setAmenity(event.target.value)} /><button onClick={() => { if (amenity.trim()) setForm({ ...form, amenities: [...form.amenities, amenity.trim()] }); setAmenity(""); }} className="rounded bg-secondary px-3">Add</button></div><div className="mt-2 flex flex-wrap gap-1">{form.amenities.map((item) => <button key={item} onClick={() => setForm({ ...form, amenities: form.amenities.filter((value) => value !== item) })} className="rounded bg-secondary px-2 py-1 text-[10px]">{item}</button>)}</div></div>
            <button disabled={saving} onClick={() => void saveExperience()} className="btn-accent mt-6 flex w-full items-center justify-center gap-2 rounded-lg py-2 disabled:opacity-50"><Save className="h-4 w-4" />{saving ? "Saving…" : editing ? "Save changes" : "Create experience"}</button>
          </div>
        </div>
      )}

      {slotExperienceId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-md rounded-2xl border border-border bg-background p-6">
            <div className="mb-5 flex items-center justify-between"><h2 className="flex items-center gap-2 text-lg font-bold"><Calendar className="h-5 w-5" /> {editingSlotId ? "Edit" : "Add"} availability</h2><button onClick={() => { setSlotExperienceId(undefined); setEditingSlotId(undefined); }}><X className="h-5 w-5" /></button></div>
            <div className="space-y-4">
              <label className="block text-xs">Starts<input type="datetime-local" className="input-glass mt-1 w-full" value={slotForm.startTimeUtc} onChange={(event) => setSlotForm({ ...slotForm, startTimeUtc: event.target.value })} /></label>
              <label className="block text-xs">Ends<input type="datetime-local" className="input-glass mt-1 w-full" value={slotForm.endTimeUtc} onChange={(event) => setSlotForm({ ...slotForm, endTimeUtc: event.target.value })} /></label>
              <label className="block text-xs">Capacity<input type="number" min="1" className="input-glass mt-1 w-full" value={slotForm.capacity} onChange={(event) => setSlotForm({ ...slotForm, capacity: Number(event.target.value) })} /></label>
              <label className="block text-xs">Price per person<input type="number" min="0" className="input-glass mt-1 w-full" value={slotForm.pricePerPerson} onChange={(event) => setSlotForm({ ...slotForm, pricePerPerson: Number(event.target.value) })} /></label>
            </div>
            <button onClick={() => void createSlot()} className="btn-accent mt-5 w-full rounded-lg py-2">{editingSlotId ? "Save slot" : "Create slot"}</button>
          </div>
        </div>
      )}
    </div>
  );
};

export default MyExperiencesTab;
