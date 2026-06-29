import { useState, useCallback } from "react";
import { Sparkles, Clock, MapPin, Car, UtensilsCrossed, Star, Loader2, RefreshCw, Plus, Trash2, Check } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";

// Vibe options with images and descriptions
const vibeOptions = [
  { id: "culture", name: "Culture & History", icon: "🏛️", image: "https://images.pexels.com/photos/235731/pexels-photo-235731.jpeg?w=400&h=300&fit=crop", description: "Museums, temples, heritage sites" },
  { id: "art", name: "Art & Museums", icon: "🎨", image: "https://images.pexels.com/photos/207896/pexels-photo-207896.jpeg?w=400&h=300&fit=crop", description: "Galleries, exhibitions, street art" },
  { id: "foodie", name: "Foodie", icon: "🍜", image: "https://images.pexels.com/photos/1267320/pexels-photo-1267320.jpeg?w=400&h=300&fit=crop", description: "Local cuisine, food tours, cooking classes" },
  { id: "nightlife", name: "Nightlife", icon: "🌙", image: "https://images.pexels.com/photos/1190297/pexels-photo-1190297.jpeg?w=400&h=300&fit=crop", description: "Bars, clubs, live music" },
  { id: "nature", name: "Nature & Relax", icon: "🌿", image: "https://images.pexels.com/photos/1687845/pexels-photo-1687845.jpeg?w=400&h=300&fit=crop", description: "Parks, beaches, spa retreats" },
  { id: "shopping", name: "Shopping", icon: "🛍️", image: "https://images.pexels.com/photos/291762/pexels-photo-291762.jpeg?w=400&h=300&fit=crop", description: "Markets, malls, boutiques" },
  { id: "family", name: "Family Friendly", icon: "👨‍👩‍👧", image: "https://images.pexels.com/photos/4543663/pexels-photo-4543663.jpeg?w=400&h=300&fit=crop", description: "Activities for all ages" },
  { id: "hidden", name: "Hidden Gems", icon: "💎", image: "https://images.pexels.com/photos/4530066/pexels-photo-4530066.jpeg?w=400&h=300&fit=crop", description: "Off the beaten path" },
  { id: "luxury", name: "Luxury", icon: "✨", image: "https://images.pexels.com/photos/258154/pexels-photo-258154.jpeg?w=400&h=300&fit=crop", description: "High-end experiences" },
];

type ItineraryItem = { time: string; icon: string; name: string; desc: string; duration: string };
type DayPlan = {
  day: string;
  items: ItineraryItem[];
  transport: string;
  cost: string;
  why: string;
};

const allActivities: Record<string, ItineraryItem[]> = {
  "Culture & History": [
    { time: "10:00 AM", icon: "🏛️", name: "Egyptian Museum", desc: "Explore ancient artifacts and royal mummies", duration: "2h" },
    { time: "1:00 PM", icon: "🕌", name: "Al-Azhar Mosque", desc: "One of the oldest mosques in the world", duration: "1h" },
    { time: "3:00 PM", icon: "🏪", name: "Khan el-Khalili Bazaar", desc: "Wander through the historic marketplace", duration: "2h" },
    { time: "5:30 PM", icon: "🏰", name: "Saladin Citadel", desc: "Medieval Islamic fortification on Mokattam Hill", duration: "1.5h" },
  ],
  "Art & Museums": [
    { time: "10:00 AM", icon: "🎨", name: "Museum of Islamic Art", desc: "World-class collection of Islamic artifacts", duration: "2h" },
    { time: "12:30 PM", icon: "🖼️", name: "Cairo Opera House Gallery", desc: "Contemporary Egyptian art exhibitions", duration: "1.5h" },
    { time: "3:00 PM", icon: "🏛️", name: "Coptic Museum", desc: "Ancient Coptic art and manuscripts", duration: "1.5h" },
    { time: "5:00 PM", icon: "📸", name: "Townhouse Gallery", desc: "Modern art space in Downtown Cairo", duration: "1h" },
  ],
  "Foodie": [
    { time: "10:00 AM", icon: "☕", name: "Café Riche", desc: "Historic café since 1908 — Egyptian breakfast", duration: "1h" },
    { time: "11:30 AM", icon: "🍽️", name: "Zooba Street Food", desc: "Modern take on Egyptian street food favorites", duration: "1h" },
    { time: "1:30 PM", icon: "🧆", name: "Abou Tarek Koshary", desc: "The most famous koshary in all of Egypt", duration: "45min" },
    { time: "3:00 PM", icon: "🍰", name: "El Abd Bakery", desc: "Legendary pastries and Egyptian desserts", duration: "30min" },
    { time: "4:00 PM", icon: "🫖", name: "Al-Fishawi Café", desc: "Traditional tea house in Khan el-Khalili", duration: "1h" },
    { time: "7:00 PM", icon: "🥘", name: "Sequoia Restaurant", desc: "Nile-side dining with stunning views", duration: "2h" },
  ],
  "Nightlife": [
    { time: "6:00 PM", icon: "🌅", name: "Cairo Tower", desc: "Sunset panoramic views of the city", duration: "1h" },
    { time: "8:00 PM", icon: "🍽️", name: "Tamarai Lounge", desc: "Upscale dining and cocktails", duration: "2h" },
    { time: "10:30 PM", icon: "🎵", name: "Cairo Jazz Club", desc: "Live music and vibrant nightlife", duration: "2h" },
    { time: "12:30 AM", icon: "🌙", name: "Nile Corniche Walk", desc: "Late-night stroll along the river", duration: "1h" },
  ],
  "Nature & Relax": [
    { time: "8:00 AM", icon: "🌴", name: "Al-Azhar Park", desc: "Beautiful green oasis in the heart of Cairo", duration: "2h" },
    { time: "10:30 AM", icon: "🚢", name: "Nile Felucca Ride", desc: "Traditional sailboat cruise on the Nile", duration: "1.5h" },
    { time: "1:00 PM", icon: "🌺", name: "Orman Botanical Garden", desc: "Lush gardens with rare plant species", duration: "1.5h" },
    { time: "4:00 PM", icon: "🧖", name: "Spa at Four Seasons", desc: "Luxury relaxation with Nile views", duration: "2h" },
  ],
  "Shopping": [
    { time: "10:00 AM", icon: "🛍️", name: "City Stars Mall", desc: "Egypt's largest shopping center", duration: "2h" },
    { time: "12:30 PM", icon: "🏪", name: "Khan el-Khalili Souq", desc: "Traditional crafts, spices, and souvenirs", duration: "2h" },
    { time: "3:00 PM", icon: "👗", name: "Zamalek Boutiques", desc: "Local designer shops on 26th July Street", duration: "1.5h" },
    { time: "5:00 PM", icon: "🎁", name: "Fair Trade Egypt Shop", desc: "Handcrafted local products and gifts", duration: "1h" },
  ],
  "Family Friendly": [
    { time: "9:00 AM", icon: "🐘", name: "Giza Zoo", desc: "One of the oldest zoos in Africa", duration: "2h" },
    { time: "11:30 AM", icon: "🏛️", name: "Children's Museum Cairo", desc: "Interactive exhibits for kids", duration: "1.5h" },
    { time: "2:00 PM", icon: "🎢", name: "Dream Park", desc: "Egypt's largest theme park", duration: "3h" },
    { time: "6:00 PM", icon: "🍕", name: "Family Dinner at Andrea", desc: "Outdoor dining in a garden setting", duration: "1.5h" },
  ],
  "Hidden Gems": [
    { time: "9:00 AM", icon: "🏚️", name: "City of the Dead", desc: "Historic necropolis with living community", duration: "1.5h" },
    { time: "11:00 AM", icon: "🕌", name: "Ibn Tulun Mosque", desc: "Oldest intact mosque in Cairo, rarely crowded", duration: "1h" },
    { time: "1:00 PM", icon: "🍽️", name: "Fasahet Somaya", desc: "Hidden local restaurant loved by Cairenes", duration: "1h" },
    { time: "3:00 PM", icon: "🎨", name: "Darb 1718", desc: "Underground arts center in Old Cairo", duration: "1.5h" },
    { time: "5:00 PM", icon: "🌆", name: "Mokattam Hill Viewpoint", desc: "Secret sunset spot overlooking all of Cairo", duration: "1h" },
  ],
  "Luxury": [
    { time: "10:00 AM", icon: "🏰", name: "Private Pyramids Tour", desc: "Exclusive guided tour with Egyptologist", duration: "3h" },
    { time: "1:30 PM", icon: "🍽️", name: "Lunch at Mena House", desc: "Fine dining with pyramid views", duration: "1.5h" },
    { time: "4:00 PM", icon: "🧖", name: "Kempinski Spa", desc: "Premium spa and wellness experience", duration: "2h" },
    { time: "7:30 PM", icon: "🚢", name: "Private Nile Dinner Cruise", desc: "Luxury dinner cruise with entertainment", duration: "3h" },
  ],
};

const transportOptions = [
  "Uber — 15 min — ~EGP 60",
  "Metro Line 2 — 25 min — ~EGP 10",
  "Private Car — 30 min — ~EGP 150",
  "Walking — 20 min — Free",
  "Uber — 25 min — ~EGP 80",
  "Taxi — 20 min — ~EGP 100",
];

const vibeReasons: Record<string, string> = {
  "Culture & History": "Selected for its cultural importance and proximity to historic landmarks. Perfect for history enthusiasts.",
  "Art & Museums": "Curated for art lovers — a mix of ancient, Islamic, and contemporary Egyptian art scenes.",
  "Foodie": "A culinary journey through Cairo's best flavors, from street food legends to fine Nile-side dining.",
  "Nightlife": "Optimized for evening experiences — from sunset views to live music and late-night vibes.",
  "Nature & Relax": "Peaceful escapes within the city — parks, river cruises, and spa retreats for total relaxation.",
  "Shopping": "From traditional souqs to modern malls — the best shopping experiences Cairo has to offer.",
  "Family Friendly": "Safe, fun activities suitable for all ages with convenient timing and kid-friendly dining.",
  "Hidden Gems": "Off-the-beaten-path discoveries that most tourists never find. Authentic Cairo experiences.",
  "Luxury": "Premium, exclusive experiences with private tours, fine dining, and world-class amenities.",
};

const restaurants = [
  { name: "Sequoia", area: "Zamalek", cuisine: "Mediterranean", price: "$$$", rating: 4.7 },
  { name: "Zooba", area: "Downtown", cuisine: "Egyptian Street Food", price: "$", rating: 4.5 },
  { name: "Andrea El Mariouteya", area: "Giza", cuisine: "Middle Eastern Grill", price: "$$", rating: 4.6 },
];

const WhereToGo = () => {
  const [selectedVibes, setSelectedVibes] = useState<string[]>([]);
  const [numDays, setNumDays] = useState(1);
  const [startTime, setStartTime] = useState("09:00");
  const [endTime, setEndTime] = useState("21:00");
  const [budget, setBudget] = useState("$$ Moderate");
  const [generatedPlan, setGeneratedPlan] = useState<DayPlan[]>([]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [hasGenerated, setHasGenerated] = useState(false);

  const toggleVibe = (vibeName: string) => {
    setSelectedVibes((prev) =>
      prev.includes(vibeName) ? prev.filter((x) => x !== vibeName) : [...prev, vibeName]
    );
  };

  const generateItinerary = useCallback(() => {
    if (selectedVibes.length === 0) return;

    setIsGenerating(true);
    setGeneratedPlan([]);

    setTimeout(() => {
      const days: DayPlan[] = [];
      const costMultiplier = budget === "$ Budget" ? 0.6 : budget === "$$$ Luxury" ? 2.0 : 1.0;

      for (let d = 0; d < Math.min(numDays, 5); d++) {
        const vibeIndex = d % selectedVibes.length;
        const vibe = selectedVibes[vibeIndex];
        const activities = allActivities[vibe] || allActivities["Culture & History"];

        const startHour = parseInt(startTime.split(":")[0]);
        const endHour = parseInt(endTime.split(":")[0]);
        const hoursAvailable = endHour - startHour;
        const maxItems = Math.min(activities.length, Math.max(2, Math.floor(hoursAvailable / 2)));
        const dayItems = activities.slice(0, maxItems);

        const baseCost = Math.round((350 + Math.random() * 500) * costMultiplier);

        days.push({
          day: `Day ${d + 1} — ${vibe}`,
          items: dayItems,
          transport: transportOptions[d % transportOptions.length],
          cost: `EGP ${baseCost}`,
          why: vibeReasons[vibe] || "Personalized based on your preferences.",
        });
      }

      setGeneratedPlan(days);
      setIsGenerating(false);
      setHasGenerated(true);
    }, 1500);
  }, [selectedVibes, numDays, startTime, endTime, budget]);

  const removeStop = (dayIndex: number, itemIndex: number) => {
    setGeneratedPlan((prev) =>
      prev.map((day, di) =>
        di === dayIndex ? { ...day, items: day.items.filter((_, ii) => ii !== itemIndex) } : day
      )
    );
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />

      <div className="container mx-auto px-4 py-10 max-w-4xl">
        <div className="text-center mb-10">
          <h1 className="text-3xl font-extrabold">
            Where to <span className="text-gradient-orange">Go</span>
          </h1>
          <p className="text-sm text-muted-foreground mt-2">Plan your perfect trip with AI-powered itineraries and local insights</p>
        </div>

        {/* Vibe Selection - Now with Image Cards */}
        <div className="card-glass p-5 mb-6">
          <h3 className="font-bold mb-1">Choose Your Vibe</h3>
          <p className="text-xs text-muted-foreground mb-4">Select one or more vibes to personalize your itinerary</p>
          
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-3 gap-4">
            {vibeOptions.map((vibe) => {
              const isSelected = selectedVibes.includes(vibe.name);
              return (
                <motion.button
                  key={vibe.id}
                  whileTap={{ scale: 0.97 }}
                  onClick={() => toggleVibe(vibe.name)}
                  className={`relative rounded-xl overflow-hidden transition-all duration-200 text-left ${
                    isSelected ? "ring-2 ring-accent ring-offset-2 ring-offset-background shadow-lg" : "hover:shadow-md"
                  }`}
                >
                  <div className="aspect-video w-full">
                    <img
                      src={vibe.image}
                      alt={vibe.name}
                      className="w-full h-full object-cover"
                      loading="lazy"
                    />
                    {isSelected && (
                      <div className="absolute top-2 right-2 w-6 h-6 rounded-full bg-accent flex items-center justify-center">
                        <Check className="w-3.5 h-3.5 text-white" />
                      </div>
                    )}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/30 to-transparent" />
                    <div className="absolute bottom-2 left-2 right-2">
                      <p className="text-white font-semibold text-sm flex items-center gap-1">
                        <span className="text-base">{vibe.icon}</span> {vibe.name}
                      </p>
                      <p className="text-white/80 text-[10px] truncate">{vibe.description}</p>
                    </div>
                  </div>
                </motion.button>
              );
            })}
          </div>

          {selectedVibes.length === 0 && hasGenerated === false && (
            <p className="text-xs text-accent mt-3">⚡ Select at least one vibe to generate your itinerary</p>
          )}
        </div>

        {/* Trip Details (unchanged) */}
        <div className="card-glass p-5 mb-4">
          <h3 className="font-bold mb-3">Trip Details</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            <div>
              <label className="text-xs text-muted-foreground mb-1 block">📅 Number of Days</label>
              <input
                type="number"
                min={1}
                max={5}
                value={numDays}
                onChange={(e) => setNumDays(Math.max(1, Math.min(5, parseInt(e.target.value) || 1)))}
                className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground mb-1 block">🕐 Start Time</label>
              <select
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              >
                {["06:00", "07:00", "08:00", "09:00", "10:00", "11:00"].map((t) => (
                  <option key={t}>{t}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground mb-1 block">🕐 End Time</label>
              <select
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              >
                {["17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:00"].map((t) => (
                  <option key={t}>{t}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs text-muted-foreground mb-1 block">💰 Budget Range</label>
              <select
                value={budget}
                onChange={(e) => setBudget(e.target.value)}
                className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
              >
                <option>$ Budget</option>
                <option>$$ Moderate</option>
                <option>$$$ Luxury</option>
              </select>
            </div>
          </div>
        </div>

        {/* Generate Button */}
        <button
          onClick={generateItinerary}
          disabled={selectedVibes.length === 0 || isGenerating}
          className={`w-full py-3 rounded-xl flex items-center justify-center gap-2 font-semibold mb-10 transition-all ${
            selectedVibes.length === 0
              ? "bg-muted text-muted-foreground cursor-not-allowed"
              : "btn-accent"
          }`}
        >
          {isGenerating ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" /> Generating your personalized plan...
            </>
          ) : (
            <>
              <Sparkles className="w-4 h-4" /> Generate Itinerary
            </>
          )}
        </button>

        {/* Generated Itinerary (unchanged) */}
        <AnimatePresence>
          {generatedPlan.length > 0 && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.3 }}
            >
              <div className="flex items-center justify-between mb-4 flex-wrap gap-2">
                <div>
                  <h2 className="text-xl font-bold">Your Personalized Trip Plan</h2>
                  <p className="text-xs text-muted-foreground">
                    Optimized for your vibe, time, and budget · {selectedVibes.join(", ")}
                  </p>
                </div>
                <button
                  onClick={generateItinerary}
                  className="flex items-center gap-1.5 px-3 py-1.5 text-xs border border-border rounded-lg hover:bg-secondary transition-colors"
                >
                  <RefreshCw className="w-3 h-3" /> Regenerate
                </button>
              </div>

              {generatedPlan.map((day, di) => (
                <motion.div
                  key={day.day}
                  initial={{ opacity: 0, y: 15 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: di * 0.15 }}
                  className="card-glass p-5 mb-4"
                >
                  <h3 className="font-bold mb-4 flex items-center gap-2">
                    <span className="w-2 h-2 rounded-full bg-primary" />
                    {day.day}
                  </h3>

                  <div className="space-y-4 sm:ml-4 sm:border-l sm:border-border/50 sm:pl-4">
                    {day.items.map((item, ii) => (
                      <div key={`${item.name}-${ii}`} className="group relative">
                        <div className="flex items-start gap-3">
                          <span className="text-xs text-muted-foreground w-12 sm:w-16 shrink-0 pt-0.5">{item.time}</span>
                          <div className="flex-1 min-w-0">
                            <h4 className="text-sm font-semibold flex items-center gap-1.5">
                              {item.icon} <span className="truncate">{item.name}</span>
                            </h4>
                            <p className="text-xs text-muted-foreground line-clamp-2">{item.desc}</p>
                            <span className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                              <Clock className="w-3 h-3" /> {item.duration}
                            </span>
                          </div>
                          <button
                            onClick={() => removeStop(di, ii)}
                            className="opacity-0 sm:group-hover:opacity-100 transition-opacity p-1 rounded hover:bg-destructive/20 shrink-0"
                            title="Remove stop"
                          >
                            <Trash2 className="w-3.5 h-3.5 text-destructive" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div className="mt-4 pt-3 border-t border-border/30">
                    <p className="text-xs text-muted-foreground flex items-center gap-2">
                      <Car className="w-3.5 h-3.5" /> {day.transport}
                    </p>
                    <div className="flex justify-between items-center mt-2">
                      <span className="text-xs text-muted-foreground">Estimated cost today:</span>
                      <span className="font-bold text-accent">{day.cost}</span>
                    </div>
                  </div>

                  <div className="mt-3 p-3 rounded-lg bg-primary/10 border border-primary/20 text-xs text-muted-foreground">
                    💡 Why this fits your vibe: {day.why}
                  </div>
                </motion.div>
              ))}

              <div className="card-glass p-4 mb-10 flex items-center justify-between">
                <span className="font-bold">Estimated Total Trip Cost</span>
                <span className="text-lg font-extrabold text-accent">
                  EGP {generatedPlan.reduce((sum, d) => sum + parseInt(d.cost.replace(/[^0-9]/g, "")), 0).toLocaleString()}
                </span>
              </div>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Recommended Places to Eat (unchanged) */}
        <h2 className="text-xl font-bold mt-6 mb-4">Recommended Places to Eat</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {restaurants.map((r) => (
            <div key={r.name} className="card-glass p-4">
              <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center mb-3">
                <UtensilsCrossed className="w-4 h-4 text-accent" />
              </div>
              <h3 className="font-bold text-sm">{r.name}</h3>
              <p className="text-xs text-muted-foreground flex items-center gap-1 mb-2">
                <MapPin className="w-3 h-3" /> {r.area}
              </p>
              <div className="space-y-1 text-xs">
                <div className="flex justify-between"><span className="text-muted-foreground">Cuisine</span><span>{r.cuisine}</span></div>
                <div className="flex justify-between"><span className="text-muted-foreground">Price Range</span><span className="text-accent">{r.price}</span></div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Rating</span>
                  <span className="flex items-center gap-1 text-gold"><Star className="w-3 h-3 fill-current" /> {r.rating}</span>
                </div>
              </div>
              <button className="w-full mt-3 py-2 rounded-lg bg-primary text-primary-foreground text-xs font-medium hover:opacity-90 transition-opacity">
                View on Map
              </button>
            </div>
          ))}
        </div>
      </div>

      <Footer />
    </div>
  );
};

export default WhereToGo;