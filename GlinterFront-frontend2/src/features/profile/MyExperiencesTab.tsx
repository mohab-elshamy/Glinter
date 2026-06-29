import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Sparkles, Plus, Edit, Trash2, Clock, DollarSign, Users, Tag, Calendar, X, Save, Eye, MapPin, Activity, Check } from "lucide-react";
import { toast } from "sonner";
import { experiencesApi } from "@/shared/services/api-experiences";
import { authStorage } from "@/shared/lib/auth";

const categoryOptions = [
  "Cruise", "Historical", "Adventure", "Cultural", "Water Sports", "Food", "Wellness", "Shopping"
];

const vibeOptions = [
  "Cultural", "Adventure", "Foodie", "Nightlife", "Nature", "Shopping", "Family", "Hidden Gems", "Luxury"
];

interface ExperienceListing {
  id: string;
  categoryId: string;
  title: string;
  description: string;
  locationName: string;
  pricePerPerson: number;
  currency: string;
  durationMinutes: number;
  maxGuests: number;
  latitude: number;
  longitude: number;
  vibeIds: string[];
  tags: string[];
  images: string[];
  isActive: boolean;
}

interface AvailabilitySlot {
  id: string;
  experienceId: string;
  startTimeUtc: string;
  endTimeUtc: string;
  capacity: number;
  isActive: boolean;
}

interface ExperienceBooking {
  id: string;
  experienceId: string;
  experienceName: string;
  travelerName: string;
  guestsCount: number;
  totalPrice: number;
  status: "pending" | "confirmed" | "completed" | "cancelled";
  bookedDate: string;
}

const MyExperiencesTab = () => {
  const [experiences, setExperiences] = useState<ExperienceListing[]>(() => {
    const saved = localStorage.getItem("my_experiences");
    return saved ? JSON.parse(saved) : [];
  });

  const [availabilitySlots, setAvailabilitySlots] = useState<AvailabilitySlot[]>(() => {
    const saved = localStorage.getItem("my_availability_slots");
    return saved ? JSON.parse(saved) : [];
  });

  const [bookings, setBookings] = useState<ExperienceBooking[]>(() => {
    const saved = localStorage.getItem("my_exp_bookings");
    return saved ? JSON.parse(saved) : [];
  });

  const [showAddModal, setShowAddModal] = useState(false);
  const [editingExp, setEditingExp] = useState<ExperienceListing | null>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [expandedExp, setExpandedExp] = useState<string | null>(null);
  const [showSlotsFor, setShowSlotsFor] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    categoryId: "",
    title: "",
    description: "",
    locationName: "",
    pricePerPerson: 0,
    currency: "EGP",
    durationMinutes: 60,
    maxGuests: 10,
    latitude: 30.0444,
    longitude: 31.2357,
    vibeIds: [] as string[],
    tags: [] as string[],
    images: [] as string[],
  });

  const [newSlot, setNewSlot] = useState({ startTime: "", endTime: "", capacity: 5 });
  const [newTag, setNewTag] = useState("");
  const [newImageUrl, setNewImageUrl] = useState("");

  useEffect(() => {
    localStorage.setItem("my_experiences", JSON.stringify(experiences));
  }, [experiences]);

  useEffect(() => {
    localStorage.setItem("my_availability_slots", JSON.stringify(availabilitySlots));
  }, [availabilitySlots]);

  useEffect(() => {
    localStorage.setItem("my_exp_bookings", JSON.stringify(bookings));
  }, [bookings]);

  // Fetch experiences from backend on mount (fall back to localStorage)
  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;
    experiencesApi.getMyExperiences()
      .then((apiExperiences) => {
        if (apiExperiences.length > 0) {
          const mapped: ExperienceListing[] = apiExperiences.map((exp) => ({
            id: exp.id,
            categoryId: exp.categoryName || exp.categoryId,
            title: exp.title,
            description: exp.description,
            locationName: exp.locationName,
            pricePerPerson: exp.pricePerPerson,
            currency: exp.currency,
            durationMinutes: exp.durationMinutes,
            maxGuests: exp.maxGuests,
            latitude: exp.latitude,
            longitude: exp.longitude,
            vibeIds: exp.vibes.map((v) => v.name),
            tags: exp.tags.map((t) => t.name),
            images: [],
            isActive: exp.isActive,
          }));
          setExperiences(mapped);
        }
      })
      .catch(() => {});
  }, []);

  const resetForm = () => {
    setFormData({
      categoryId: "",
      title: "",
      description: "",
      locationName: "",
      pricePerPerson: 0,
      currency: "EGP",
      durationMinutes: 60,
      maxGuests: 10,
      latitude: 30.0444,
      longitude: 31.2357,
      vibeIds: [],
      tags: [],
      images: [],
    });
    setNewTag("");
    setNewImageUrl("");
  };

  const toggleVibe = (vibe: string) => {
    setFormData(prev => ({
      ...prev,
      vibeIds: prev.vibeIds.includes(vibe) ? prev.vibeIds.filter(v => v !== vibe) : [...prev.vibeIds, vibe]
    }));
  };

  const handleCreate = () => {
    if (!formData.title || !formData.categoryId || !formData.locationName || formData.pricePerPerson <= 0) {
      toast.error("Title, category, location, and price are required");
      return;
    }
    const newExp: ExperienceListing = {
      id: `exp-${Date.now()}`,
      ...formData,
      isActive: true,
    };
    setExperiences([newExp, ...experiences]);
    setShowAddModal(false);
    resetForm();
    toast.success(`"${newExp.title}" created successfully! 🎉`);

    // Also try to save to backend
    if (authStorage.isAuthenticated()) {
      experiencesApi.createExperience({
        categoryId: "",
        areaId: "00000000-0000-0000-0000-000000000000",
        title: formData.title,
        description: formData.description,
        locationName: formData.locationName,
        pricePerPerson: formData.pricePerPerson,
        currency: formData.currency,
        durationMinutes: formData.durationMinutes,
        maxGuests: formData.maxGuests,
        latitude: formData.latitude,
        longitude: formData.longitude,
        vibeIds: [],
        tags: formData.tags,
      }).catch(() => {});
    }
  };

  const handleEdit = (exp: ExperienceListing) => {
    setEditingExp(exp);
    setFormData({
      categoryId: exp.categoryId,
      title: exp.title,
      description: exp.description,
      locationName: exp.locationName,
      pricePerPerson: exp.pricePerPerson,
      currency: exp.currency,
      durationMinutes: exp.durationMinutes,
      maxGuests: exp.maxGuests,
      latitude: exp.latitude,
      longitude: exp.longitude,
      vibeIds: exp.vibeIds,
      tags: exp.tags,
      images: exp.images,
    });
    setShowEditModal(true);
  };

  const handleSaveEdit = () => {
    if (!editingExp) return;
    setExperiences(prev =>
      prev.map(e => e.id === editingExp.id ? { ...e, ...formData } : e)
    );
    setShowEditModal(false);
    setEditingExp(null);
    resetForm();
    toast.success("Experience updated successfully! ✅");
  };

  const handleDelete = (id: string, title: string) => {
    setExperiences(prev => prev.filter(e => e.id !== id));
    setAvailabilitySlots(prev => prev.filter(s => s.experienceId !== id));
    toast.error(`"${title}" has been removed`);
  };

  const handleToggleActive = (id: string) => {
    setExperiences(prev =>
      prev.map(e => e.id === id ? { ...e, isActive: !e.isActive } : e)
    );
  };

  const handleAddSlot = (experienceId: string) => {
    if (!newSlot.startTime || !newSlot.endTime || newSlot.capacity <= 0) {
      toast.error("Start time, end time, and capacity are required");
      return;
    }
    const slot: AvailabilitySlot = {
      id: `slot-${Date.now()}`,
      experienceId,
      startTimeUtc: new Date(newSlot.startTime).toISOString(),
      endTimeUtc: new Date(newSlot.endTime).toISOString(),
      capacity: newSlot.capacity,
      isActive: true,
    };
    setAvailabilitySlots([...availabilitySlots, slot]);
    setNewSlot({ startTime: "", endTime: "", capacity: 5 });
    toast.success("Availability slot added! 📅");
  };

  const handleToggleSlot = (slotId: string) => {
    setAvailabilitySlots(prev =>
      prev.map(s => s.id === slotId ? { ...s, isActive: !s.isActive } : s)
    );
  };

  const handleDeleteSlot = (slotId: string) => {
    setAvailabilitySlots(prev => prev.filter(s => s.id !== slotId));
  };

  const handleUpdateBookingStatus = (bookingId: string, status: "confirmed" | "completed" | "cancelled") => {
    setBookings(prev => prev.map(b => b.id === bookingId ? { ...b, status } : b));
    toast.success(`Booking ${status}!`);
  };

  const expSlots = (expId: string) => availabilitySlots.filter(s => s.experienceId === expId);
  const expBookings = (expId: string) => bookings.filter(b => b.experienceId === expId);

  const formatDate = (isoString: string) => {
    try {
      return new Date(isoString).toLocaleString();
    } catch {
      return isoString;
    }
  };

  return (
    <>
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Sparkles className="w-6 h-6 text-accent" />
          <div>
            <h1 className="text-2xl font-bold">My Experiences</h1>
            <p className="text-xs text-muted-foreground">{experiences.length} experience{experiences.length !== 1 ? "s" : ""}</p>
          </div>
        </div>
        <button
          onClick={() => { resetForm(); setShowAddModal(true); }}
          className="btn-accent px-4 py-2 rounded-lg text-sm flex items-center gap-2"
        >
          <Plus className="w-4 h-4" /> Add Experience
        </button>
      </div>

      {experiences.length === 0 ? (
        <div className="card-glass p-12 text-center">
          <Activity className="w-16 h-16 text-muted-foreground mx-auto mb-4 opacity-30" />
          <h2 className="text-lg font-bold mb-2">No experiences yet</h2>
          <p className="text-sm text-muted-foreground mb-4">Start by adding your first experience listing</p>
          <button
            onClick={() => { resetForm(); setShowAddModal(true); }}
            className="btn-accent px-5 py-2 rounded-lg text-sm"
          >
            <Plus className="w-4 h-4 inline mr-1" /> Add Your First Experience
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {experiences.map((exp) => (
            <motion.div
              key={exp.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="card-glass p-5"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <h3 className="font-bold text-lg">{exp.title}</h3>
                    <span className={`text-[10px] px-2 py-0.5 rounded-full ${
                      exp.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"
                    }`}>
                      {exp.isActive ? "Active" : "Inactive"}
                    </span>
                    <span className="text-[10px] bg-accent/20 text-accent px-2 py-0.5 rounded-full">{exp.categoryId}</span>
                  </div>
                  <p className="text-xs text-muted-foreground flex items-center gap-1 mb-2">
                    <MapPin className="w-3 h-3" /> {exp.locationName}
                  </p>
                  <div className="flex items-center gap-4 text-sm mb-3">
                    <span className="text-accent font-bold">${exp.pricePerPerson}<span className="text-xs text-muted-foreground font-normal">/{exp.currency}/person</span></span>
                    <span className="text-muted-foreground flex items-center gap-1"><Clock className="w-3 h-3" /> {exp.durationMinutes} min</span>
                    <span className="text-muted-foreground flex items-center gap-1"><Users className="w-3 h-3" /> Max {exp.maxGuests}</span>
                  </div>
                  <div className="flex flex-wrap gap-1.5">
                    {exp.vibeIds.map(vibe => (
                      <span key={vibe} className="text-[10px] bg-accent/15 text-accent px-2 py-0.5 rounded-full">{vibe}</span>
                    ))}
                    {exp.tags.map(tag => (
                      <span key={tag} className="text-[10px] bg-secondary px-2 py-0.5 rounded-full">{tag}</span>
                    ))}
                  </div>
                </div>
                <div className="flex gap-1">
                  <button onClick={() => handleEdit(exp)} className="p-2 hover:bg-secondary rounded-lg transition-colors" title="Edit">
                    <Edit className="w-4 h-4 text-yellow-500" />
                  </button>
                  <button onClick={() => handleToggleActive(exp.id)} className="p-2 hover:bg-secondary rounded-lg transition-colors" title="Toggle active">
                    <Eye className="w-4 h-4 text-blue-500" />
                  </button>
                  <button onClick={() => handleDelete(exp.id, exp.title)} className="p-2 hover:bg-secondary rounded-lg transition-colors" title="Delete">
                    <Trash2 className="w-4 h-4 text-red-500" />
                  </button>
                </div>
              </div>

              {exp.description && (
                <p className="text-xs text-muted-foreground mt-3 line-clamp-2">{exp.description}</p>
              )}

              <div className="flex gap-4 mt-3">
                <button
                  onClick={() => setExpandedExp(expandedExp === exp.id ? null : exp.id)}
                  className="text-xs text-accent hover:underline"
                >
                  {expandedExp === exp.id ? "Hide" : "View"} Bookings ({expBookings(exp.id).length})
                </button>
                <button
                  onClick={() => setShowSlotsFor(showSlotsFor === exp.id ? null : exp.id)}
                  className="text-xs text-accent hover:underline"
                >
                  {showSlotsFor === exp.id ? "Hide" : "Manage"} Availability ({expSlots(exp.id).length} slots)
                </button>
              </div>

              {showSlotsFor === exp.id && (
                <div className="mt-3 pt-3 border-t border-border">
                  <div className="flex gap-2 mb-3">
                    <input
                      type="datetime-local"
                      value={newSlot.startTime}
                      onChange={(e) => setNewSlot({ ...newSlot, startTime: e.target.value })}
                      className="flex-1 px-2 py-1 rounded bg-secondary border border-border text-xs"
                    />
                    <input
                      type="datetime-local"
                      value={newSlot.endTime}
                      onChange={(e) => setNewSlot({ ...newSlot, endTime: e.target.value })}
                      className="flex-1 px-2 py-1 rounded bg-secondary border border-border text-xs"
                    />
                    <input
                      type="number"
                      min={1}
                      value={newSlot.capacity}
                      onChange={(e) => setNewSlot({ ...newSlot, capacity: parseInt(e.target.value) || 1 })}
                      className="w-16 px-2 py-1 rounded bg-secondary border border-border text-xs"
                      placeholder="Cap"
                    />
                    <button onClick={() => handleAddSlot(exp.id)} className="px-3 py-1 bg-accent rounded text-xs whitespace-nowrap">
                      + Add Slot
                    </button>
                  </div>
                  {expSlots(exp.id).length === 0 ? (
                    <p className="text-xs text-muted-foreground text-center py-2">No availability slots yet. Add one above.</p>
                  ) : (
                    <div className="space-y-1">
                      {expSlots(exp.id).map(slot => (
                        <div key={slot.id} className="flex items-center justify-between p-2 bg-secondary/30 rounded text-xs">
                          <div className="flex items-center gap-2">
                            <Calendar className="w-3 h-3 text-muted-foreground" />
                            <span>{formatDate(slot.startTimeUtc)} → {formatDate(slot.endTimeUtc)}</span>
                            <span className="text-muted-foreground">Cap: {slot.capacity}</span>
                            <span className={`px-1.5 py-0.5 rounded-full ${
                              slot.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"
                            }`}>{slot.isActive ? "Active" : "Inactive"}</span>
                          </div>
                          <div className="flex gap-1">
                            <button onClick={() => handleToggleSlot(slot.id)} className="text-blue-500 hover:text-blue-400">
                              <Eye className="w-3 h-3" />
                            </button>
                            <button onClick={() => handleDeleteSlot(slot.id)} className="text-red-500 hover:text-red-400">
                              <Trash2 className="w-3 h-3" />
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {expandedExp === exp.id && (
                <div className="mt-3 pt-3 border-t border-border">
                  {expBookings(exp.id).length === 0 ? (
                    <p className="text-xs text-muted-foreground text-center py-3">No bookings for this experience yet</p>
                  ) : (
                    <div className="space-y-2">
                      {expBookings(exp.id).map(b => (
                        <div key={b.id} className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-xs">
                          <div>
                            <p className="font-medium">{b.travelerName}</p>
                            <p className="text-muted-foreground">{b.guestsCount} guest{b.guestsCount > 1 ? "s" : ""} · {b.bookedDate}</p>
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
                          {b.status === "confirmed" && (
                            <button onClick={() => handleUpdateBookingStatus(b.id, "completed")} className="text-xs text-blue-500 hover:text-blue-400 ml-2">
                              Complete
                            </button>
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

      {/* Add/Edit Experience Modal */}
      {(showAddModal || showEditModal) && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-background border border-border rounded-2xl max-w-lg w-full max-h-[90vh] overflow-y-auto p-6">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-xl font-bold">{showEditModal ? "Edit Experience" : "Add New Experience"}</h2>
              <button
                onClick={() => { setShowAddModal(false); setShowEditModal(false); setEditingExp(null); resetForm(); }}
                className="p-1 hover:bg-secondary rounded"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Title *</label>
                <input
                  value={formData.title}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                  placeholder="e.g. Sunset Nile Cruise"
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Category *</label>
                <select
                  value={formData.categoryId}
                  onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                >
                  <option value="">Select a category...</option>
                  {categoryOptions.map(cat => (
                    <option key={cat} value={cat}>{cat}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Location Name *</label>
                <input
                  value={formData.locationName}
                  onChange={(e) => setFormData({ ...formData, locationName: e.target.value })}
                  placeholder="e.g. Nile River, Cairo"
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                />
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-1 block">Description</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Describe the experience..."
                  rows={3}
                  className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary resize-none"
                />
              </div>

              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Price/Person *</label>
                  <input
                    type="number"
                    min={0}
                    value={formData.pricePerPerson || ""}
                    onChange={(e) => setFormData({ ...formData, pricePerPerson: parseFloat(e.target.value) || 0 })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Duration (min)</label>
                  <input
                    type="number"
                    min={1}
                    value={formData.durationMinutes}
                    onChange={(e) => setFormData({ ...formData, durationMinutes: parseInt(e.target.value) || 60 })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Max Guests</label>
                  <input
                    type="number"
                    min={1}
                    value={formData.maxGuests}
                    onChange={(e) => setFormData({ ...formData, maxGuests: parseInt(e.target.value) || 1 })}
                    className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
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
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">Coordinates</label>
                  <div className="flex gap-2">
                    <input
                      type="number" step="any"
                      value={formData.latitude}
                      onChange={(e) => setFormData({ ...formData, latitude: parseFloat(e.target.value) || 0 })}
                      placeholder="Lat"
                      className="w-full px-2 py-2 rounded-lg bg-secondary border border-border text-xs focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                    <input
                      type="number" step="any"
                      value={formData.longitude}
                      onChange={(e) => setFormData({ ...formData, longitude: parseFloat(e.target.value) || 0 })}
                      placeholder="Lng"
                      className="w-full px-2 py-2 rounded-lg bg-secondary border border-border text-xs focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </div>
                </div>
              </div>

              <div>
                <label className="text-xs text-muted-foreground mb-2 block">Vibes</label>
                <div className="flex gap-2 flex-wrap">
                  {vibeOptions.map(vibe => (
                    <button
                      key={vibe}
                      onClick={() => toggleVibe(vibe)}
                      className={`text-xs px-3 py-1.5 rounded-full transition-all flex items-center gap-1 ${
                        formData.vibeIds.includes(vibe)
                          ? "bg-accent text-accent-foreground"
                          : "bg-secondary text-muted-foreground hover:bg-accent/20"
                      }`}
                    >
                      {vibe}
                      {formData.vibeIds.includes(vibe) && <Check className="w-3 h-3" />}
                    </button>
                  ))}
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
                  <button onClick={() => { if (newImageUrl && !formData.images.includes(newImageUrl)) { setFormData({ ...formData, images: [...formData.images, newImageUrl] }); setNewImageUrl(""); } }} className="px-3 py-2 bg-accent rounded-lg text-sm">Add</button>
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
                <div className="flex gap-2">
                  <input
                    value={newTag}
                    onChange={(e) => setNewTag(e.target.value)}
                    placeholder="Add a tag..."
                    className="flex-1 px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
                  />
                  <button onClick={() => { if (newTag && !formData.tags.includes(newTag)) { setFormData({ ...formData, tags: [...formData.tags, newTag] }); setNewTag(""); } }} className="px-3 py-2 bg-accent rounded-lg text-sm">Add</button>
                </div>
                <div className="flex flex-wrap gap-1.5 mt-2">
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
                <Save className="w-4 h-4" /> {showEditModal ? "Save Changes" : "Create Experience"}
              </button>
              <button
                onClick={() => { setShowAddModal(false); setShowEditModal(false); setEditingExp(null); resetForm(); }}
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

export default MyExperiencesTab;
