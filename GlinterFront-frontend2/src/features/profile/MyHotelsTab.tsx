import { useEffect, useState } from "react";
import { Building2, Edit, Eye, EyeOff, ImagePlus, MapPin, Plus, Save, X } from "lucide-react";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import { formatUsdPrice } from "@/shared/lib/price";
import type {
  CreateStayRequest,
  StayBookingResponseDto,
  StayBookingStatus,
  StayResponseDto,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import LeafletMap from "@/components/LeafletMap";
import { regionsApi } from "@/shared/services/api-regions";

const emptyForm: CreateStayRequest = {
  name: "",
  price: 0,
  description: "",
  googleMapsLink: "",
  website: "",
  phoneInternational: "",
  locationSummaryDescription: "",
  latitude: undefined,
  longitude: undefined,
  imageLinks: [],
  amenities: [],
  bookingPlatforms: [],
};

function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : "Something went wrong.";
}

const MyHotelsTab = () => {
  const [hotels, setHotels] = useState<StayResponseDto[]>([]);
  const [bookings, setBookings] = useState<Record<number, StayBookingResponseDto[]>>({});
  const [expandedId, setExpandedId] = useState<number>();
  const [editing, setEditing] = useState<StayResponseDto>();
  const [form, setForm] = useState<CreateStayRequest>(emptyForm);
  const [imageUrl, setImageUrl] = useState("");
  const [amenity, setAmenity] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [saving, setSaving] = useState(false);
  const [uploadingImages, setUploadingImages] = useState(false);
  const [locatingRegion, setLocatingRegion] = useState(false);

  const loadHotels = async () => {
    try {
      setHotels(await staysApi.getMyStays());
      setLoadState({ status: "ready" });
    } catch (error) {
      const message = errorMessage(error);
      setLoadState({ status: "error", message });
      toast.error(message);
    }
  };

  useEffect(() => {
    void loadHotels();
  }, []);

  const openCreate = () => {
    setEditing(undefined);
    setForm({ ...emptyForm, imageLinks: [], amenities: [], bookingPlatforms: [] });
    setShowForm(true);
  };

  const openEdit = (hotel: StayResponseDto) => {
    setEditing(hotel);
    setForm({
      name: hotel.name,
      price: hotel.price ?? 0,
      description: hotel.description ?? "",
      googleMapsLink: hotel.googleMapsLink ?? "",
      website: hotel.website ?? "",
      phoneInternational: hotel.phoneInternational ?? "",
      locationSummaryDescription: hotel.locationSummaryDescription ?? "",
      latitude: hotel.latitude,
      longitude: hotel.longitude,
      adm0Gid: hotel.adm0Gid,
      adm1Gid: hotel.adm1Gid,
      adm2Gid: hotel.adm2Gid,
      adm3Gid: hotel.adm3Gid,
      imageLinks: hotel.images.map((image) => image.link),
      amenities: hotel.amenities,
      bookingPlatforms: hotel.bookingPlatforms.map(({ name, priceWithTax, link }) => ({
        name,
        priceWithTax,
        link,
      })),
    });
    setShowForm(true);
  };

  const save = async () => {
    if (!form.name.trim() || form.price <= 0 || form.latitude == null || form.longitude == null) {
      toast.error("Name, a positive nightly price, and a map location are required.");
      return;
    }

    setSaving(true);
    try {
      const saved = editing
        ? await staysApi.updateStay(editing.id, form)
        : await staysApi.createStay(form);

      setHotels((current) =>
        editing
          ? current.map((hotel) => (hotel.id === saved.id ? saved : hotel))
          : [saved, ...current],
      );
      setShowForm(false);
      toast.success(editing ? "Stay updated." : "Stay created.");
    } catch (error) {
      toast.error(errorMessage(error));
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
      toast.error(errorMessage(error));
    } finally {
      setLocatingRegion(false);
    }
  };

  const uploadImages = async (files: FileList | null) => {
    if (!files?.length) return;
    setUploadingImages(true);
    try {
      const uploaded = await Promise.all(
        Array.from(files).map((file) => staysApi.uploadImage(file)),
      );
      setForm((current) => ({
        ...current,
        imageLinks: [...current.imageLinks, ...uploaded.map((image) => image.link)],
      }));
      toast.success(`${uploaded.length} image${uploaded.length === 1 ? "" : "s"} uploaded.`);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setUploadingImages(false);
    }
  };

  const toggleActive = async (hotel: StayResponseDto) => {
    try {
      const updated = hotel.isActive
        ? await staysApi.deactivateStay(hotel.id)
        : await staysApi.activateStay(hotel.id);
      setHotels((current) => current.map((item) => (item.id === updated.id ? updated : item)));
      toast.success(updated.isActive ? "Stay activated." : "Stay deactivated.");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const toggleBookings = async (hotelId: number) => {
    if (expandedId === hotelId) {
      setExpandedId(undefined);
      return;
    }

    setExpandedId(hotelId);
    try {
      const loadedBookings = await staysApi.getBookings(hotelId);
      setBookings((current) => ({ ...current, [hotelId]: loadedBookings }));
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const updateBooking = async (hotelId: number, bookingId: string, status: StayBookingStatus) => {
    try {
      const updated = status === "Cancelled"
        ? await staysApi.cancelBooking(bookingId)
        : await staysApi.updateBookingStatus(bookingId, status);
      setBookings((current) => ({
        ...current,
        [hotelId]: (current[hotelId] ?? []).map((booking) =>
          booking.id === updated.id ? updated : booking),
      }));
      toast.success(`Booking ${status.toLowerCase()}.`);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  if (loadState.status === "loading") {
    return <p className="text-sm text-muted-foreground">Loading your stays…</p>;
  }
  if (loadState.status === "error") {
    return <p className="rounded-lg border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">{loadState.message}</p>;
  }

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Building2 className="h-6 w-6 text-accent" />
          <div>
            <h1 className="text-2xl font-bold">My Hotels</h1>
            <p className="text-xs text-muted-foreground">{hotels.length} backend listing(s)</p>
          </div>
        </div>
        <button onClick={openCreate} className="btn-accent flex items-center gap-2 rounded-lg px-4 py-2 text-sm">
          <Plus className="h-4 w-4" /> Add Hotel
        </button>
      </div>

      {hotels.length === 0 ? (
        <div className="card-glass p-12 text-center">
          <Building2 className="mx-auto mb-4 h-16 w-16 opacity-30" />
          <p className="mb-4 text-sm text-muted-foreground">No stays have been created for this account.</p>
          <button onClick={openCreate} className="btn-accent rounded-lg px-5 py-2 text-sm">Add your first stay</button>
        </div>
      ) : (
        <div className="space-y-4">
          {hotels.map((hotel) => (
            <section key={hotel.id} className="card-glass p-5">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <div className="mb-2 flex items-center gap-2">
                    <h3 className="text-lg font-bold">{hotel.name}</h3>
                    <span className={`rounded-full px-2 py-0.5 text-[10px] ${hotel.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"}`}>
                      {hotel.isActive ? "Active" : "Inactive"}
                    </span>
                  </div>
                  <p className="flex items-center gap-1 text-xs text-muted-foreground">
                    <MapPin className="h-3 w-3" /> {hotel.locationSummaryDescription || "No location description"}
                  </p>
                  <p className="mt-2 font-bold text-accent">
                    {formatUsdPrice(hotel.price)}{hotel.price != null && hotel.price > 0 ? "/night" : ""}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-1">
                    {hotel.amenities.map((item) => <span key={item} className="rounded-full bg-secondary px-2 py-1 text-[10px]">{item}</span>)}
                  </div>
                </div>
                <div className="flex gap-1">
                  <button onClick={() => openEdit(hotel)} className="rounded-lg p-2 hover:bg-secondary" title="Edit"><Edit className="h-4 w-4" /></button>
                  <button onClick={() => void toggleActive(hotel)} className="rounded-lg p-2 hover:bg-secondary" title={hotel.isActive ? "Deactivate" : "Activate"}>
                    {hotel.isActive ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
              </div>
              <button onClick={() => void toggleBookings(hotel.id)} className="mt-4 text-xs text-accent hover:underline">
                {expandedId === hotel.id ? "Hide" : "View"} bookings
              </button>
              {expandedId === hotel.id && (
                <div className="mt-3 space-y-2 border-t border-border pt-3">
                  {(bookings[hotel.id] ?? []).length === 0 && <p className="text-xs text-muted-foreground">No bookings yet.</p>}
                  {(bookings[hotel.id] ?? []).map((booking) => (
                    <div key={booking.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg bg-secondary/30 p-3 text-xs">
                      <div>
                        <p className="font-medium">{booking.guestName}</p>
                        <p className="text-muted-foreground">{booking.checkInDate} → {booking.checkOutDate} · {booking.guestCount} guest(s)</p>
                      </div>
                      <div className="text-right">
                        <p className="font-bold text-accent">{formatUsdPrice(booking.totalPrice)}</p>
                        <p>{booking.status}</p>
                      </div>
                      {booking.status === "Pending" && (
                        <div className="flex gap-2">
                          <button onClick={() => void updateBooking(hotel.id, booking.id, "Confirmed")} className="text-green-400">Confirm</button>
                          <button onClick={() => void updateBooking(hotel.id, booking.id, "Cancelled")} className="text-red-400">Cancel</button>
                        </div>
                      )}
                      {booking.status === "Confirmed" && (
                        <button onClick={() => void updateBooking(hotel.id, booking.id, "Completed")} className="text-blue-400">Complete</button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </section>
          ))}
        </div>
      )}

      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-2xl border border-border bg-background p-6">
            <div className="mb-5 flex items-center justify-between">
              <h2 className="text-xl font-bold">{editing ? "Edit stay" : "Create stay"}</h2>
              <button onClick={() => setShowForm(false)}><X className="h-5 w-5" /></button>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="text-xs">Name *<input className="input-glass mt-1 w-full" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
              <label className="text-xs">Price/night (USD) *<input type="number" min="1" className="input-glass mt-1 w-full" value={form.price} onChange={(event) => setForm({ ...form, price: Number(event.target.value) })} /></label>
              <label className="text-xs md:col-span-2">Description<textarea className="input-glass mt-1 w-full" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
              <div className="md:col-span-2">
                <RegionCascadeSelect
                  value={form}
                  onChange={(selection) => setForm({ ...form, ...selection })}
                  label="Backend region"
                />
              </div>
              <label className="text-xs md:col-span-2">Location summary<input className="input-glass mt-1 w-full" value={form.locationSummaryDescription} onChange={(event) => setForm({ ...form, locationSummaryDescription: event.target.value })} /></label>
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
                        name: form.name || "Selected stay location",
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
              <label className="text-xs">Website<input className="input-glass mt-1 w-full" value={form.website} onChange={(event) => setForm({ ...form, website: event.target.value })} /></label>
              <label className="text-xs">Phone<input className="input-glass mt-1 w-full" value={form.phoneInternational} onChange={(event) => setForm({ ...form, phoneInternational: event.target.value })} /></label>
              <label className="text-xs md:col-span-2">Google Maps link<input className="input-glass mt-1 w-full" value={form.googleMapsLink} onChange={(event) => setForm({ ...form, googleMapsLink: event.target.value })} /></label>
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
                <button onClick={() => { if (imageUrl.trim()) setForm({ ...form, imageLinks: [...form.imageLinks, imageUrl.trim()] }); setImageUrl(""); }} className="rounded bg-secondary px-3">Add</button>
              </div>
              <div className="mt-3 grid grid-cols-3 gap-2 sm:grid-cols-4">
                {form.imageLinks.map((url) => (
                  <button
                    key={url}
                    onClick={() => setForm({ ...form, imageLinks: form.imageLinks.filter((item) => item !== url) })}
                    className="group relative overflow-hidden rounded-lg border border-border"
                    title="Click to remove"
                  >
                    <img src={url} alt="" className="h-20 w-full object-cover" />
                    <span className="absolute inset-0 hidden items-center justify-center bg-black/60 text-xs group-hover:flex">Remove</span>
                  </button>
                ))}
              </div>
            </div>
            <div className="mt-5">
              <p className="mb-2 text-xs">Amenities</p>
              <div className="flex gap-2"><input className="input-glass flex-1" value={amenity} onChange={(event) => setAmenity(event.target.value)} placeholder="Wi-Fi" /><button onClick={() => { if (amenity.trim()) setForm({ ...form, amenities: [...form.amenities, amenity.trim()] }); setAmenity(""); }} className="rounded bg-secondary px-3">Add</button></div>
              <div className="mt-2 flex flex-wrap gap-1">{form.amenities.map((item) => <button key={item} onClick={() => setForm({ ...form, amenities: form.amenities.filter((value) => value !== item) })} className="rounded bg-secondary px-2 py-1 text-[10px]" title="Click to remove">{item}</button>)}</div>
            </div>

            <button disabled={saving} onClick={() => void save()} className="btn-accent mt-6 flex w-full items-center justify-center gap-2 rounded-lg py-2 disabled:opacity-50">
              <Save className="h-4 w-4" /> {saving ? "Saving…" : editing ? "Save changes" : "Create stay"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default MyHotelsTab;
