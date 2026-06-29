export const vibeOptions = [
  "Culture & History", "Art & Museums", "Foodie", "Nightlife",
  "Nature & Relax", "Shopping", "Family Friendly", "Hidden Gems", "Luxury",
];

export interface ItineraryItem {
  time: string;
  icon: string;
  name: string;
  desc: string;
  duration: string;
}

export interface DayPlan {
  day: string;
  items: ItineraryItem[];
  transport: string;
  cost: string;
  why: string;
}

export interface Restaurant {
  name: string;
  area: string;
  cuisine: string;
  price: string;
  rating: number;
}

export const allActivities: Record<string, ItineraryItem[]> = {
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

export const transportOptions = [
  "Uber — 15 min — ~EGP 60",
  "Metro Line 2 — 25 min — ~EGP 10",
  "Private Car — 30 min — ~EGP 150",
  "Walking — 20 min — Free",
  "Uber — 25 min — ~EGP 80",
  "Taxi — 20 min — ~EGP 100",
];

export const vibeReasons: Record<string, string> = {
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

export const restaurants: Restaurant[] = [
  { name: "Sequoia", area: "Zamalek", cuisine: "Mediterranean", price: "$$$", rating: 4.7 },
  { name: "Zooba", area: "Downtown", cuisine: "Egyptian Street Food", price: "$", rating: 4.5 },
  { name: "Andrea El Mariouteya", area: "Giza", cuisine: "Middle Eastern Grill", price: "$$", rating: 4.6 },
];
