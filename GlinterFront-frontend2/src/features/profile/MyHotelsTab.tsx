import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Building2, Plus, Edit, Trash2, MapPin, DollarSign, Users, Tag, X, Save, Eye } from "lucide-react";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import { authStorage } from "@/shared/lib/auth";
import type { StayResponseDto } from "@/shared/types/api";

interface HotelListing {
  id: string;
  name: string;
  description: string;
  address: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  tags: string[];
  images: string[];
  latitude: number;
  longitude: number;
  isActive: boolean;
}

interface HotelBooking {
  id: string;
  hotelId: string;
  hotelName: string;
  guestName: string;
  checkIn: string;
  checkOut: string;
  guestCount: number;
  totalPrice: number;
  status: "pending" | "confirmed" | "completed" | "cancelled";
}

const defaultTags = ["wifi", "parking", "restaurant", "pool", "spa", "airport-shuttle", "breakfast", "ac", "seaview", "pet-friendly"];

const MyHotelsTab = () => {
  const [hotels, setHotels] = useState<HotelListing[]>(() => {
    const saved = localStorage.getItem("my_hotels");
    return saved ? JSON.parse(saved) : [];
  });

  const [bookings, setBookings] = useState<HotelBooking[]>(() => {
    const saved = localStorage.getItem("my_hotel_bookings");
    return saved ? JSON.parse(saved) : [];
  });

  const [showAddModal, setShowAddModal] = useState(false);
  const [editingHotel, setEditingHotel] = useState<HotelListing | null>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [expandedHotel, setExpandedHotel] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: "",
    description: "",
    address: "",
    pricePerNight: 0,
    currency: "EGP",
    maxGuests: 2,
    tags: [] as string[],
    images: [] as string[],
    latitude: 30.0444,
    longitude: 31.2357,
  });

  const [newImageUrl, setNewImageUrl] = useState("");
  const [newTag, setNewTag] = useState("");

  useEffect(() => {
    localStorage.setItem("my_hotels", JSON.stringify(hotels));
  }, [hotels]);

  useEffect(() => {
    localStorage.setItem("my_hotel_bookings", JSON.stringify(bookings));
  }, [bookings]);

  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;

    staysApi.getStays()
      .then(apiStays => {
        const mapped: HotelListing[] = apiStays.map(s => ({
          id: s.id,
          name: s.name,
          description: (s as StayResponseDto).description || "",
          address: s.address,
          pricePerNight: s.pricePerNight,
          currency: s.currency,
          maxGuests: s.maxGuests,
          tags: s.amenities || [],
          images: s.images || [],
          latitude: (s as StayResponseDto).latitude || 30.0444,
          longitude: (s as StayResponseDto).longitude || 31.2357,
          isActive: s.isActive,
        }));
        setHotels(prev => {
          const existing = new Set(prev.map(h => h.id));
          const newOnes = mapped.filter(h => !existing.has(h.id));
          return [...newOnes, ...prev];
        });
      })
      .catch(() => {});
  }, []);

  const resetForm = () => {
    setFormData({
      name: "",
      description: "",
      address: "",
      pricePerNight: 0,
      currency: "EGP",
      maxGuests: 2,
      tags: [],
      images: [],
      latitude: 30.0444,
      longitude: 31.2357,
    });
    setNewImageUrl("");
    setNewTag("");
  };

  const handleAddImage = () => {
    if (newImageUrl && !formData.images.includes(newImageUrl)) {
      setFormData({ ...formData, images: [...formData.images, newImageUrl] });
      setNewImageUrl("");
    }
  };

  const handleAddTag = () => {
    if (newTag && !formData.tags.includes(newTag)) {
      setFormData({ ...formData, tags: [...formData.tags, newTag] });
      setNewTag("");
    }
  };

  const handleCreate = async () => {
    if (!formData.name || !formData.address || formData.pricePerNight <= 0) {
      toast.error("Name, address, and price are required");
      return;
    }
    const newHotel: HotelListing = {
      id: `hotel-${Date.now()}`,
      ...formData,
      isActive: true,
    };

    if (authStorage.isAuthenticated()) {
      try {
        const created = await staysApi.createStay({
          areaId: "00000000-0000-0000-0000-000000000000",
          name: formData.name,
          description: formData.description,
          address: formData.address,
          pricePerNight: formData.pricePerNight,
          currency: formData.currency,
          maxGuests: formData.maxGuests,
          latitude: formData.latitude,
          longitude: formData.longitude,
          tags: formData.tags,
          amenities: formData.tags,
          images: formData.images,
        });
        newHotel.id = created.id;
      } catch { }
    }

    setHotels([newHotel, ...hotels]);
    setShowAddModal(false);
    resetForm();
    toast.success(`"${newHotel.name}" created successfully! 🎉`);
  };

  const handleEdit = (hotel: HotelListing) => {
    setEditingHotel(hotel);
    setFormData({
      name: hotel.name,
      description: hotel.description,
      address: hotel.address,
      pricePerNight: hotel.pricePerNight,
      currency: hotel.currency,
      maxGuests: hotel.maxGuests,
      tags: hotel.tags,
      images: hotel.images,
      latitude: hotel.latitude,
      longitude: hotel.longitude,
    });
    setShowEditModal(true);
  };

  const handleSaveEdit = async () => {
    if (!editingHotel) return;

    if (authStorage.isAuthenticated()) {
      try {
        const uuid = editingHotel.id;
        if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(uuid)) {
          await staysApi.updateStay(uuid, {
            name: formData.name,
            description: formData.description,
            address: formData.address,
            pricePerNight: formData.pricePerNight,
            currency: formData.currency,
            maxGuests: formData.maxGuests,
            latitude: formData.latitude,
            longitude: formData.longitude,
            tags: formData.tags,
            amenities: formData.tags,
            images: formData.images,
          });
        }
      } catch { }
    }

    setHotels(prev =>
      prev.map(h => h.id === editingHotel.id ? { ...h, ...formData } : h)
    );
    setShowEditModal(false);
    setEditingHotel(null);
    resetForm();
    toast.success("Hotel updated successfully! ✅");
  };

  const handleDelete = async (id: string, name: string) => {
    if (authStorage.isAuthenticated()) {
      try {
        if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)) {
          await staysApi.deactivateStay(id);
        }
      } catch { }
    }
    setHotels(prev => prev.filter(h => h.id !== id));
    toast.error(`"${name}" has been removed`);
  };

  const handleToggleActive = async (id: string) => {
    if (authStorage.isAuthenticated()) {
      try {
        if (/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)) {
          const hotel = hotels.find(h => h.id === id);
          if (hotel?.isActive) {
            await staysApi.deactivateStay(id);
          } else {
            await staysApi.activateStay(id);
          }
        }
      } catch { }
    }
    setHotels(prev =>
      prev.map(h => h.id === id ? { ...h, isActive: !h.isActive } : h)
    );
  };

  const handleUpdateBookingStatus = (bookingId: string, status: "confirmed" | "completed" | "cancelled") => {
    setBookings(prev => prev.map(b => b.id === bookingId ? { ...b, status } : b));
    toast.success(`Booking ${status}!`);
  };

  const hotelBookings = (hotelId: string) => bookings.filter(b => b.hotelId === hotelId);

  return (
    <>
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Building2 className="w-6 h-6 text-accent" />
          <div>
            <h1 className="text-2xl font-bold">My Hotels</h1>
            <p className="text-xs text-muted-foreground">{hotels.length} listing{hotels.length !== 1 ? "s" : ""}</p>
          </div>
        </div>
        <button
          onClick={() => { resetForm(); setShowAddModal(true); }}
          className="btn-accent px-4 py-2 rounded-lg text-sm flex items-center gap-2"
        >
          <Plus className="w-4 h-4" /> Add Hotel
        </button>
      </div>

      {hotels.length === 0 ? (
        <div className="card-glass p-12 text-center">
          <Building2 className="w-16 h-16 text-muted-foreground mx-auto mb-4 opacity-30" />
          <h2 className="text-lg font-bold mb-2">No hotels yet</h2>
          <p className="text-sm text-muted-foreground mb-4">Start by adding your first hotel listing</p>
          <button
            onClick={() => { resetForm(); setShowAddModal(true); }}
            className="btn-accent px-5 py-2 rounded-lg text-sm"
          >
            <Plus className="w-4 h-4 inline mr-1" /> Add Your First Hotel
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {hotels.map((hotel) => (
            <motion.div
              key={hotel.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="card-glass p-5"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="font-bold text-lg">{hotel.name}</h3>
                    <span className={`text-[10px] px-2 py-0.5 rounded-full ${
                      hotel.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"
                    }`}>
                      {hotel.isActive ? "Active" : "Inactive"}
                    </span>
                  </div>
                  <p className="text-xs text-muted-foreground flex items-center gap-1 mb-2">
                    <MapPin className="w-3 h-3" /> {hotel.address}
                  </p>
                  <div className="flex items-center gap-4 text-sm mb-3">
                    <span className="text-accent font-bold">${hotel.pricePerNight}<span className="text-xs text-muted-foreground font-normal">/{hotel.currency}/night</span></span>
                    <span className="text-muted-foreground flex items-center gap-1"><Users className="w-3 h-3" /> Max {hotel.maxGuests} guests</span>
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {hotel.tags.map(tag => (
                      <span key={tag} className="text-[10px] bg-secondary px-2 py-0.5 rounded-full">{tag}</span>
                    ))}
                  </div>
                </div>
                <div className="flex gap-1">
                  <button onClick={() => handleEdit(hotel)} className="p-2 hover:bg-secondary rounded-lg transition-colors" title="Edit">
                    <Edit className="w-4 h-4 text-yellow-500" />
                  </button>
                  <button onClick={() => handleToggleActive(hotel.id)} className="p-2 hover:bg-secondary rounded-lg transition-colors" title="Toggle active">
                    <Eye className="w-4 h-4 text-blue-500" />
                  </button>
                  <button onClick={() => handleDelete(hotel.id, hotel.name)} className="p-2 hover:bg-secondary rounded-lg transition-colors" title="Delete">
                    <Trash2 className="w-4 h-4 text-red-500" />
                  </button>
                </div>
              </div>

              {hotel.description && (
                <p className="text-xs text-muted-foreground mt-3 line-clamp-2">{hotel.description}</p>
              )}

              <div className="mt-3">
                <button
                  onClick={() => setExpandedHotel(expandedHotel === hotel.id ? null : hotel.id)}
                  className="text-xs text-accent hover:underline"
                >
                  {expandedHotel === hotel.id ? "Hide" : "View"} Bookings ({hotelBookings(hotel.id).length})
                </button>
              </div>

              {expandedHotel === hotel.id && (
                <div className="mt-3 pt-3 border-t border-border">
                  {hotelBookings(hotel.id).length === 0 ? (
                    <p className="text-xs text-muted-foreground text-center py-3">No bookings for this hotel yet</p>
                  ) : (
                    <div className="space-y-2">
                      {hotelBookings(hotel.id).map(b => (
                        <div key={b.id} className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-xs">
                          <div>
                            <p className="font-medium">{b.guestName}</p>
                            <p className="text-muted-foreground">{b.checkIn} → {b.checkOut} · {b.guestCount} guest{b.guestCount > 1 ? "s" : ""}</p>
                          </div>
                          <div className="text-right">
                            <p className="font-bold text-accent">${b.totalPrice}</p>
                            <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${
                              b.status === "confirmed" ? "bg-green-500/20 text-green-400" :
                              b.status === "pending" ? "bg-yellow-500/20 text-yellow-400" :
                              b.status === "completed" ? "bg-blue-500/20 text-blue-400" :
                              "bg-red-500/20 text-red-400"
                            }`}>{b.status}</span>
                          </div>
                          {b.status === "pending" && (
                            <div className="flex gap-1 ml-2">
                              <button onClick={() => handleUpdateBookingStatus(b.id, "confirmed")} className="text-green-500 hover:text-green-400">✓</button>
                              <button onClick={() => handleUpdateBookingStatus(b.id, "cancelled")} className="text-red-500 hover:text-red-400">✕</button>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </motion.div>
          ))}
        </div>
      )}

      {/* Add/Edit Hotel Modal */}
      {(showAddModal || showEditModal) && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-background border border-border rounded-2xl max-w-lg w-full max-h-[90vh] overflow-y-auto p-6">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-xl font-bold">{showEditModal ? "Edit Hotel" : "Add New Hotel"}</h2>
              <button
                onClick={() => { setShowAddModal(false); setShowEditModal(false); setEditingHotel(null); resetForm(); }}
                className="p-1 hover:bg-secondary rounded"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Hotel Name *</label>
                <input
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g. Nile Palace Hotel"
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Description</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Describe your hotel..."
                  rows={3}
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary resize-none"
                />
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Address *</label>
                <input
                  value={formData.address}
                  onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                  placeholder="e.g. 15 Nile Street, Cairo"
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Price Per Night *</label>
                  <input
                    type="number"
                    min={0}
                    value={formData.pricePerNight || ""}
                    onChange={(e) => setFormData({ ...formData, pricePerNight: parseFloat(e.target.value) || 0 })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Currency</label>
                  <select
                    value={formData.currency}
                    onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    <option>EGP</option>
                    <option>USD</option>
                    <option>EUR</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Max Guests *</label>
                <input
                  type="number"
                  min={1}
                  value={formData.maxGuests}
                  onChange={(e) => setFormData({ ...formData, maxGuests: parseInt(e.target.value) || 1 })}
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Latitude</label>
                  <input
                    type="number"
                    step="any"
                    value={formData.latitude}
                    onChange={(e) => setFormData({ ...formData, latitude: parseFloat(e.target.value) || 0 })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Longitude</label>
                  <input
                    type="number"
                    step="any"
                    value={formData.longitude}
                    onChange={(e) => setFormData({ ...formData, longitude: parseFloat(e.target.value) || 0 })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Images</label>
                <div className="flex gap-2 mb-2">
                  <input
                    value={newImageUrl}
                    onChange={(e) => setNewImageUrl(e.target.value)}
                    placeholder="Paste image URL..."
                    className="flex-1 px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                  <button onClick={handleAddImage} className="px-3 py-2 bg-accent rounded-lg text-sm">Add</button>
                </div>
                <div className="flex flex-wrap gap-2">
                  {formData.images.map((img, idx) => (
                    <div key={idx} className="relative group">
                      <img src={img} alt="" className="w-16 h-16 rounded-lg object-cover" />
                      <button
                        onClick={() => setFormData({ ...formData, images: formData.images.filter((_, i) => i !== idx) })}
                        className="absolute -top-1 -right-1 w-5 h-5 rounded-full bg-red-500 text-white flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                      >
                        <X className="w-3 h-3" />
                      </button>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Tags</label>
                <div className="flex gap-2 mb-2">
                  <select
                    value={newTag}
                    onChange={(e) => setNewTag(e.target.value)}
                    className="flex-1 px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  >
                    <option value="">Select a tag...</option>
                    {defaultTags.filter(t => !formData.tags.includes(t)).map(t => (
                      <option key={t} value={t}>{t}</option>
                    ))}
                  </select>
                  <button onClick={handleAddTag} disabled={!newTag} className="px-3 py-2 bg-accent rounded-lg text-sm">Add</button>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {formData.tags.map(tag => (
                    <span key={tag} className="text-xs bg-secondary px-2 py-1 rounded-full flex items-center gap-1">
                      {tag}
                      <button onClick={() => setFormData({ ...formData, tags: formData.tags.filter(t => t !== tag) })}>
                        <X className="w-3 h-3" />
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            </div>

            <div className="flex gap-3 mt-6">
              <button
                onClick={showEditModal ? handleSaveEdit : handleCreate}
                className="flex-1 bg-accent text-accent-foreground py-2.5 rounded-xl font-medium hover:opacity-90 transition-opacity flex items-center justify-center gap-2"
              >
                <Save className="w-4 h-4" /> {showEditModal ? "Save Changes" : "Create Hotel"}
              </button>
              <button
                onClick={() => { setShowAddModal(false); setShowEditModal(false); setEditingHotel(null); resetForm(); }}
                className="px-4 py-2.5 border border-border rounded-xl hover:bg-secondary transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default MyHotelsTab;
