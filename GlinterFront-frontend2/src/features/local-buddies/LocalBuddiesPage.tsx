import { useState, useEffect } from "react";
import { Search, Filter, Star, MapPin, MessageSquare, Heart, Languages, Trash2, HeartOff, Clock, Calendar, X } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { toast } from "sonner";
import { experiencesApi } from "@/shared/services/api-experiences";
import { authStorage } from "@/shared/lib/auth";
import type { VibeResponseDto, ExperienceCategoryResponseDto, ExperienceSummaryDto } from "@/shared/types/api";

const filters = ["All", "Favorites", "Free", "Verified", "Top Rated", "Available Now"];

// --- BUDDIES DATA (unchanged) ---
const buddies = [
  { 
    name: "Ahmed Hassan", 
    location: "Luxor", 
    rating: 4.9, 
    reviews: 127, 
    price: "Free", 
    languages: "Arabic, English", 
    interests: ["History", "Photography"], 
    verified: true,
    photo: "https://randomuser.me/api/portraits/men/1.jpg",
    bio: "Passionate about Egyptian history and photography.",
    id: 1
  },
  { 
    name: "Sara Mohamed", 
    location: "Cairo", 
    rating: 4.8, 
    reviews: 89, 
    price: "$25/hr", 
    languages: "Arabic, English, French", 
    interests: ["Food", "Culture"], 
    verified: true,
    photo: "https://randomuser.me/api/portraits/women/2.jpg",
    bio: "Food lover and culture enthusiast.",
    id: 2
  },
  { 
    name: "Omar Ali", 
    location: "Aswan", 
    rating: 4.7, 
    reviews: 56, 
    price: "$20/hr", 
    languages: "Arabic, English", 
    interests: ["Adventure", "Desert"], 
    verified: true,
    photo: "https://randomuser.me/api/portraits/men/3.jpg",
    bio: "Adventure seeker and desert expert.",
    id: 3
  },
  { 
    name: "Fatma Ibrahim", 
    location: "Alexandria", 
    rating: 4.9, 
    reviews: 203, 
    price: "Free", 
    languages: "Arabic, English, German", 
    interests: ["Beach", "History"], 
    verified: true,
    photo: "https://randomuser.me/api/portraits/women/4.jpg",
    bio: "Beach lover and history guide.",
    id: 4
  },
  { 
    name: "Karim Nasser", 
    location: "Sharm El Sheikh", 
    rating: 4.6, 
    reviews: 45, 
    price: "$30/hr", 
    languages: "Arabic, English", 
    interests: ["Diving", "Snorkeling"], 
    verified: true,
    photo: "https://randomuser.me/api/portraits/men/5.jpg",
    bio: "Professional diver and marine life expert.",
    id: 5
  },
  { 
    name: "Nadia El-Sayed", 
    location: "Dahab", 
    rating: 4.8, 
    reviews: 78, 
    price: "$15/hr", 
    languages: "Arabic, English, Spanish", 
    interests: ["Yoga", "Wellness"], 
    verified: true,
    photo: "https://randomuser.me/api/portraits/women/6.jpg",
    bio: "Yoga instructor and wellness coach.",
    id: 6
  },
];

// --- EXPERIENCES DATA with fixed Pyramids photo ---
const experiences = [
  {
    id: 101,
    name: "Sunset Nile Cruise",
    location: "Cairo",
    rating: 4.9,
    reviews: 234,
    price: "$45",
    duration: "3 hours",
    image: "https://images.pexels.com/photos/258117/pexels-photo-258117.jpeg?w=400&h=300&fit=crop",
    category: "Cruise",
    description: "Enjoy a relaxing dinner cruise on the Nile with live entertainment. Includes traditional Egyptian food and a folklore show.",
    highlights: ["Dinner included", "Live music", "Sunset views", "Folklore show"]
  },
  {
    id: 102,
    name: "Pyramids & Sphinx Tour",
    location: "Giza",
    rating: 5.0,
    reviews: 512,
    price: "$60",
    duration: "4 hours",
    // ✅ FIXED: working Pyramids of Giza photo
    image: "https://images.pexels.com/photos/1450360/pexels-photo-1450360.jpeg?w=400&h=300&fit=crop",
    category: "Historical",
    description: "Guided tour of the Great Pyramids and the Sphinx with an Egyptologist. Explore the ancient wonders and learn about pharaohs.",
    highlights: ["Expert guide", "Entry fees included", "Photo stops", "Water provided"]
  },
  {
    id: 103,
    name: "Desert Safari & Quad Biking",
    location: "Hurghada",
    rating: 4.7,
    reviews: 189,
    price: "$50",
    duration: "5 hours",
    image: "https://images.pexels.com/photos/1658967/pexels-photo-1658967.jpeg?w=400&h=300&fit=crop",
    category: "Adventure",
    description: "Thrilling desert safari with quad biking, camel ride, and Bedouin dinner under the stars.",
    highlights: ["Quad biking", "Camel ride", "BBQ dinner", "Bedouin tea"]
  },
  {
    id: 104,
    name: "Luxor Hot Air Balloon",
    location: "Luxor",
    rating: 4.9,
    reviews: 312,
    price: "$90",
    duration: "2 hours",
    image: "https://images.pexels.com/photos/3274842/pexels-photo-3274842.jpeg?w=400&h=300&fit=crop",
    category: "Adventure",
    description: "Fly over the Valley of the Kings at sunrise for breathtaking views of ancient temples and desert landscapes.",
    highlights: ["Sunrise flight", "Spectacular views", "Certificate included", "Pickup service"]
  },
  {
    id: 105,
    name: "Traditional Cooking Class",
    location: "Cairo",
    rating: 4.8,
    reviews: 97,
    price: "$35",
    duration: "3 hours",
    image: "https://images.pexels.com/photos/262978/pexels-photo-262978.jpeg?w=400&h=300&fit=crop",
    category: "Cultural",
    description: "Learn to cook authentic Egyptian dishes with a local chef. Master koshari, molokhia, and more.",
    highlights: ["All ingredients", "Recipe booklet", "Lunch included", "Hands-on experience"]
  },
  {
    id: 106,
    name: "Red Sea Snorkeling Trip",
    location: "Sharm El Sheikh",
    rating: 4.8,
    reviews: 203,
    price: "$40",
    duration: "6 hours",
    image: "https://images.pexels.com/photos/1891974/pexels-photo-1891974.jpeg?w=400&h=300&fit=crop",
    category: "Water Sports",
    description: "Explore vibrant coral reefs and marine life in the Red Sea. Perfect for beginners and experts.",
    highlights: ["Equipment provided", "Lunch on boat", "Professional guide", "Two snorkeling stops"]
  },
  {
    id: 107,
    name: "Alexandria History Walk",
    location: "Alexandria",
    rating: 4.6,
    reviews: 78,
    price: "$25",
    duration: "2.5 hours",
    image: "https://images.pexels.com/photos/2034851/pexels-photo-2034851.jpeg?w=400&h=300&fit=crop",
    category: "Historical",
    description: "Walk through the ancient streets of Alexandria with a historian. Visit the Library, Roman amphitheater, and more.",
    highlights: ["Library of Alexandria", "Roman amphitheater", "Local snacks", "Small group"]
  },
  {
    id: 108,
    name: "White Desert Camping",
    location: "Farafra",
    rating: 4.9,
    reviews: 45,
    price: "$120",
    duration: "2 days",
    image: "https://images.pexels.com/photos/2422265/pexels-photo-2422265.jpeg?w=400&h=300&fit=crop",
    category: "Adventure",
    description: "Overnight camping in the otherworldly White Desert. Includes Bedouin dinner, stargazing, and sunrise photography.",
    highlights: ["Stargazing", "Bedouin dinner", "Sunrise photography", "Camping gear"]
  }
];

const LocalBuddies = () => {
  const navigate = useNavigate();
  const [activeFilter, setActiveFilter] = useState("All");
  const [activeTab, setActiveTab] = useState<"buddies" | "experiences">("buddies");
  const [likedBuddies, setLikedBuddies] = useState<number[]>([]);
  const [likedExperiences, setLikedExperiences] = useState<number[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedExperience, setSelectedExperience] = useState<{
    id: number; name: string; location: string; rating: number; reviews: number;
    price: string; duration: string; image: string; category: string;
    description: string; highlights: string[];
  } | null>(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);

  const [apiExperiences, setApiExperiences] = useState<ExperienceSummaryDto[]>([]);
  const [apiCategories, setApiCategories] = useState<ExperienceCategoryResponseDto[]>([]);
  const [apiVibes, setApiVibes] = useState<VibeResponseDto[]>([]);

  // Load liked items from localStorage
  useEffect(() => {
    const savedBuddies = localStorage.getItem("likedBuddies");
    if (savedBuddies) setLikedBuddies(JSON.parse(savedBuddies));
    const savedExperiences = localStorage.getItem("likedExperiences");
    if (savedExperiences) setLikedExperiences(JSON.parse(savedExperiences));
  }, []);

  // Fetch experiences, categories, vibes from backend
  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;
    experiencesApi.getExperiences()
      .then(setApiExperiences)
      .catch(() => {});
    experiencesApi.getCategories()
      .then(setApiCategories)
      .catch(() => {});
    experiencesApi.getVibes()
      .then(setApiVibes)
      .catch(() => {});
  }, []);

  useEffect(() => {
    localStorage.setItem("likedBuddies", JSON.stringify(likedBuddies));
    localStorage.setItem("likedExperiences", JSON.stringify(likedExperiences));
  }, [likedBuddies, likedExperiences]);

  // Like handler
  const handleLike = (id: number, name: string, type: "buddy" | "experience") => {
    if (type === "buddy") {
      const isLiked = likedBuddies.includes(id);
      setLikedBuddies(prev => isLiked ? prev.filter(i => i !== id) : [...prev, id]);
      toast.success(isLiked ? `Removed ${name} from favorites` : `Added ${name} to favorites ❤️`);
    } else {
      const isLiked = likedExperiences.includes(id);
      setLikedExperiences(prev => isLiked ? prev.filter(i => i !== id) : [...prev, id]);
      toast.success(isLiked ? `Removed ${name} from favorites` : `Added ${name} to favorites ❤️`);
    }
  };

  const handleRemoveAllFavorites = () => {
    if (activeTab === "buddies") {
      setLikedBuddies([]);
      toast.success("All buddies removed from favorites");
    } else {
      setLikedExperiences([]);
      toast.success("All experiences removed from favorites");
    }
  };

  // Buddy actions
  const handleConnect = (buddy: typeof buddies[0]) => {
    localStorage.setItem("selectedBuddy", JSON.stringify({
      id: buddy.name,
      name: buddy.name,
      photo: buddy.photo,
      status: "online"
    }));
    toast.success(`Connecting with ${buddy.name}...`);
    navigate("/messages", { 
      state: { 
        selectedBuddy: {
          name: buddy.name,
          photo: buddy.photo,
          status: "online",
          lastSeen: "Online now"
        }
      } 
    });
  };

  const handleViewProfile = (buddy: typeof buddies[0]) => {
    navigate(`/profile/${buddy.id}`, { state: { buddy } });
  };

  // Experience actions
  const handleBookExperience = (exp: typeof experiences[0]) => {
    const amount = parseInt(exp.price.replace(/[^0-9]/g, "")) || 45;

    const consumerBooking = {
      id: `booking-${Date.now()}`,
      type: "experience" as const,
      name: exp.name,
      date: new Date().toISOString().split("T")[0],
      status: "confirmed" as const,
      amount,
    };

    const ownerBooking = {
      id: `exp-booking-${Date.now()}`,
      experienceId: `exp-${exp.id}`,
      experienceName: exp.name,
      travelerName: localStorage.getItem("profile_displayName") || "Guest",
      guestsCount: 1,
      totalPrice: amount,
      status: "pending" as const,
      bookedDate: new Date().toISOString().split("T")[0],
    };

    const existingConsumer = JSON.parse(localStorage.getItem("my_bookings") || "[]");
    existingConsumer.push(consumerBooking);
    localStorage.setItem("my_bookings", JSON.stringify(existingConsumer));

    const existingOwner = JSON.parse(localStorage.getItem("my_exp_bookings") || "[]");
    existingOwner.push(ownerBooking);
    localStorage.setItem("my_exp_bookings", JSON.stringify(existingOwner));

    toast.success(`"${exp.name}" booked successfully! 🎉`, { duration: 4000 });
  };

  const handleViewDetails = (exp: typeof experiences[0]) => {
    setSelectedExperience(exp);
    setShowDetailsModal(true);
  };

  // Fallback sand image (only if other images fail)
  const FALLBACK_SAND_IMAGE = "https://cdn.pixabay.com/photo/2013/07/18/20/26/sand-164878_640.jpg";

  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement, Event>) => {
    e.currentTarget.src = FALLBACK_SAND_IMAGE;
  };

  // Filtering logic
  const getFilteredBuddies = () => {
    let filtered = buddies;
    if (searchQuery) {
      filtered = filtered.filter(buddy => 
        buddy.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        buddy.location.toLowerCase().includes(searchQuery.toLowerCase()) ||
        buddy.interests.some(interest => interest.toLowerCase().includes(searchQuery.toLowerCase()))
      );
    }
    switch (activeFilter) {
      case "Favorites": filtered = filtered.filter(buddy => likedBuddies.includes(buddy.id)); break;
      case "Free": filtered = filtered.filter(buddy => buddy.price === "Free"); break;
      case "Verified": filtered = filtered.filter(buddy => buddy.verified); break;
      case "Top Rated": filtered = filtered.filter(buddy => buddy.rating >= 4.8); break;
      case "Available Now": filtered = filtered.filter(buddy => buddy.verified); break;
      default: break;
    }
    return filtered;
  };

  // Map API experiences to display format + merge with hardcoded
  const allExperiences = [
    ...experiences,
    ...apiExperiences.map((exp, idx) => ({
      id: 1000 + idx,
      name: exp.title,
      location: exp.locationName,
      rating: 0,
      reviews: 0,
      price: `$${exp.pricePerPerson}`,
      duration: `${exp.durationMinutes} min`,
      image: "",
      category: exp.categoryName || "General",
      description: "",
      highlights: [...exp.vibes.map((v: string) => v), ...exp.tags.map((t: string) => t)],
    })),
  ];

  const getFilteredExperiences = () => {
    let filtered = allExperiences;
    if (searchQuery) {
      filtered = filtered.filter(exp =>
        exp.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        exp.location.toLowerCase().includes(searchQuery.toLowerCase()) ||
        exp.category.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }
    switch (activeFilter) {
      case "Favorites": filtered = filtered.filter(exp => likedExperiences.includes(exp.id)); break;
      case "Free": filtered = filtered.filter(exp => exp.price === "Free" || exp.price === "$0"); break;
      case "Top Rated": filtered = filtered.filter(exp => exp.rating >= 4.8); break;
      default: break;
    }
    return filtered;
  };

  const filteredBuddies = getFilteredBuddies();
  const filteredExperiences = getFilteredExperiences();
  const favoriteCount = activeTab === "buddies" ? likedBuddies.length : likedExperiences.length;

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="container mx-auto px-4 py-8">
        <div className="flex justify-between items-start mb-4">
          <div>
            <h1 className="text-2xl font-bold text-gradient-purple mb-1">Local Buddies & Experiences</h1>
            <p className="text-muted-foreground text-sm">Connect with locals and discover authentic experiences</p>
          </div>
          {favoriteCount > 0 && (
            <motion.div 
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              className="flex items-center gap-2 bg-card border border-border rounded-lg px-3 py-2"
            >
              <Heart className="w-4 h-4 fill-red-500 text-red-500" />
              <span className="text-sm font-medium">{favoriteCount} Favorite{favoriteCount !== 1 ? 's' : ''}</span>
              <button
                onClick={handleRemoveAllFavorites}
                className="text-xs text-red-500 hover:text-red-400 transition-colors ml-1"
              >
                Clear all
              </button>
            </motion.div>
          )}
        </div>

        {/* Search */}
        <div className="flex gap-3 mb-4">
          <div className="flex-1 flex items-center gap-2 bg-card border border-border rounded-xl px-4 py-2.5">
            <Search className="w-4 h-4 text-muted-foreground" />
            <input 
              placeholder="Search by location, interest, or name..." 
              className="flex-1 bg-transparent text-sm placeholder:text-muted-foreground focus:outline-none"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            {searchQuery && (
              <button
                onClick={() => setSearchQuery("")}
                className="text-xs text-muted-foreground hover:text-foreground"
              >
                Clear
              </button>
            )}
          </div>
          <button className="flex items-center gap-2 px-4 py-2.5 border border-border rounded-xl text-sm hover:bg-card transition-colors">
            <Filter className="w-4 h-4" /> Filters
          </button>
        </div>

        {/* Filter chips */}
        <div className="flex gap-2 mb-6 overflow-x-auto pb-2">
          {filters.map((f) => (
            <button
              key={f}
              onClick={() => setActiveFilter(f)}
              className={`px-3 py-1.5 rounded-full text-xs font-medium transition-all whitespace-nowrap flex items-center gap-1 ${
                activeFilter === f
                  ? "bg-primary text-primary-foreground"
                  : "bg-secondary text-muted-foreground hover:text-foreground"
              }`}
            >
              {f === "Favorites" && <Heart className="w-3 h-3" />}
              {f}
              {f === "Favorites" && favoriteCount > 0 && (
                <span className="ml-1 px-1.5 py-0.5 bg-primary-foreground/20 rounded-full text-[10px]">
                  {favoriteCount}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* Tabs */}
        <div className="flex rounded-lg bg-secondary p-1 mb-8 max-w-xs">
          <button
            onClick={() => setActiveTab("buddies")}
            className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 ${
              activeTab === "buddies" ? "bg-primary text-primary-foreground" : "text-muted-foreground"
            }`}
          >
            👥 Local Buddies
          </button>
          <button
            onClick={() => setActiveTab("experiences")}
            className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 ${
              activeTab === "experiences" ? "bg-primary text-primary-foreground" : "text-muted-foreground"
            }`}
          >
            🎯 Experiences
          </button>
        </div>

        {/* Results count */}
        {activeTab === "buddies" && (
          <p className="text-xs text-muted-foreground mb-4">
            Showing {filteredBuddies.length} of {buddies.length} buddies
          </p>
        )}
        {activeTab === "experiences" && (
          <p className="text-xs text-muted-foreground mb-4">
            Showing {filteredExperiences.length} of {allExperiences.length} experiences
          </p>
        )}

        {/* BUDDIES CARDS (unchanged) */}
        {activeTab === "buddies" && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <AnimatePresence>
              {filteredBuddies.map((b, i) => (
                <motion.div
                  key={b.id}
                  initial={{ opacity: 0, y: 15 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, scale: 0.8 }}
                  transition={{ delay: i * 0.05 }}
                  className="card-glass p-5 hover:shadow-lg transition-shadow relative"
                >
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex items-center gap-3">
                      <div className="relative">
                        <img 
                          src={b.photo} 
                          alt={b.name}
                          className="w-11 h-11 rounded-full object-cover"
                          onError={handleImageError}
                        />
                        {b.verified && (
                          <div className="absolute -bottom-0.5 -right-0.5 w-4 h-4 rounded-full bg-green-500 border-2 border-card flex items-center justify-center">
                            <span className="text-[8px] text-white">✓</span>
                          </div>
                        )}
                      </div>
                      <div>
                        <h3 className="font-semibold text-sm">{b.name}</h3>
                        <p className="text-xs text-muted-foreground flex items-center gap-1">
                          <MapPin className="w-3 h-3" /> {b.location}
                        </p>
                      </div>
                    </div>
                    <motion.button
                      whileTap={{ scale: 0.8 }}
                      onClick={(e) => handleLike(b.id, b.name, "buddy")}
                      className="focus:outline-none group relative"
                    >
                      <Heart 
                        className={`w-5 h-5 transition-all duration-300 ${
                          likedBuddies.includes(b.id) 
                            ? "fill-red-500 text-red-500" 
                            : "text-muted-foreground group-hover:text-red-500"
                        }`}
                      />
                      {likedBuddies.includes(b.id) && (
                        <motion.div
                          initial={{ scale: 0 }}
                          animate={{ scale: 1 }}
                          className="absolute -top-1 -right-1 w-2 h-2 bg-red-500 rounded-full"
                        />
                      )}
                    </motion.button>
                  </div>

                  <div className="flex items-center gap-3 mb-3 text-xs">
                    <span className="flex items-center gap-1 text-gold">
                      <Star className="w-3 h-3 fill-current" /> {b.rating} ({b.reviews})
                    </span>
                    {b.price === "Free" ? (
                      <span className="bg-green-500/20 text-green-400 px-2 py-0.5 rounded-full font-medium">Free</span>
                    ) : (
                      <span className="text-accent font-medium">{b.price}</span>
                    )}
                  </div>

                  <p className="text-xs text-muted-foreground flex items-center gap-1 mb-2">
                    <Languages className="w-3 h-3" /> {b.languages}
                  </p>

                  <div className="flex flex-wrap gap-1.5 mb-4">
                    {b.interests.map((int) => (
                      <span key={int} className="text-xs bg-secondary px-2 py-0.5 rounded font-medium">{int}</span>
                    ))}
                  </div>

                  <div className="flex gap-2">
                    <button 
                      onClick={() => handleConnect(b)}
                      className="flex-1 btn-accent text-xs py-2 rounded-lg flex items-center justify-center gap-1.5"
                    >
                      <MessageSquare className="w-3.5 h-3.5" /> Connect
                    </button>
                    <button 
                      onClick={() => handleViewProfile(b)}
                      className="px-4 py-2 text-xs border border-border rounded-lg hover:bg-card transition-colors font-medium"
                    >
                      View Profile
                    </button>
                  </div>

                  {likedBuddies.includes(b.id) && (
                    <motion.div 
                      initial={{ opacity: 0, x: -10 }}
                      animate={{ opacity: 1, x: 0 }}
                      className="absolute top-3 right-12"
                    >
                      <div className="bg-red-500/20 text-red-500 text-[10px] px-1.5 py-0.5 rounded-full flex items-center gap-1">
                        <Heart className="w-2 h-2 fill-red-500" />
                        Liked
                      </div>
                    </motion.div>
                  )}
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        )}

        {/* EXPERIENCES CARDS */}
        {activeTab === "experiences" && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <AnimatePresence>
              {filteredExperiences.map((exp, i) => (
                <motion.div
                  key={exp.id}
                  initial={{ opacity: 0, y: 15 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, scale: 0.8 }}
                  transition={{ delay: i * 0.05 }}
                  className="card-glass p-0 overflow-hidden hover:shadow-lg transition-shadow relative"
                >
                  <div className="relative h-40 w-full overflow-hidden">
                    <img 
                      src={exp.image} 
                      alt={exp.name}
                      className="w-full h-full object-cover hover:scale-105 transition-transform duration-300"
                      onError={handleImageError}
                    />
                    <div className="absolute top-2 right-2 z-10">
                      <motion.button
                        whileTap={{ scale: 0.8 }}
                        onClick={(e) => {
                          e.stopPropagation();
                          handleLike(exp.id, exp.name, "experience");
                        }}
                        className="p-1.5 bg-black/50 rounded-full backdrop-blur-sm"
                      >
                        <Heart 
                          className={`w-4 h-4 ${
                            likedExperiences.includes(exp.id) 
                              ? "fill-red-500 text-red-500" 
                              : "text-white"
                          }`}
                        />
                      </motion.button>
                    </div>
                    <div className="absolute bottom-2 left-2 bg-black/60 text-white text-xs px-2 py-0.5 rounded-full">
                      {exp.category}
                    </div>
                  </div>
                  
                  <div className="p-5">
                    <h3 className="font-bold text-base mb-2">{exp.name}</h3>
                    
                    <div className="flex items-center gap-2 text-xs text-muted-foreground mb-2">
                      <MapPin className="w-3 h-3" />
                      <span>{exp.location}</span>
                      <span className="mx-1">•</span>
                      <Clock className="w-3 h-3" />
                      <span>{exp.duration}</span>
                    </div>

                    <div className="flex items-center gap-3 mb-3">
                      <span className="flex items-center gap-1 text-gold text-xs">
                        <Star className="w-3 h-3 fill-current" /> {exp.rating} ({exp.reviews})
                      </span>
                      <span className="text-accent font-bold">{exp.price}</span>
                    </div>

                    <p className="text-xs text-muted-foreground line-clamp-2 mb-3">
                      {exp.description}
                    </p>

                    <div className="flex flex-wrap gap-1.5 mb-4">
                      {exp.highlights.slice(0, 2).map((highlight, idx) => (
                        <span key={idx} className="text-xs bg-secondary px-2 py-0.5 rounded font-medium">
                          {highlight}
                        </span>
                      ))}
                      {exp.highlights.length > 2 && (
                        <span className="text-xs bg-secondary px-2 py-0.5 rounded font-medium">
                          +{exp.highlights.length - 2}
                        </span>
                      )}
                    </div>

                    <div className="flex gap-2">
                      <button 
                        onClick={() => handleBookExperience(exp)}
                        className="flex-1 btn-accent text-xs py-2 rounded-lg flex items-center justify-center gap-1.5"
                      >
                        <Calendar className="w-3.5 h-3.5" /> Book Now
                      </button>
                      <button 
                        onClick={() => handleViewDetails(exp)}
                        className="px-4 py-2 text-xs border border-border rounded-lg hover:bg-card transition-colors font-medium"
                      >
                        Details
                      </button>
                    </div>
                  </div>

                  {likedExperiences.includes(exp.id) && (
                    <div className="absolute top-2 left-2">
                      <div className="bg-red-500/90 text-white text-[10px] px-2 py-0.5 rounded-full flex items-center gap-1">
                        <Heart className="w-2 h-2 fill-white" />
                        Liked
                      </div>
                    </div>
                  )}
                </motion.div>
              ))}
            </AnimatePresence>
          </div>
        )}

        {/* No results messages (unchanged) */}
        {activeTab === "buddies" && filteredBuddies.length === 0 && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="text-center py-12"
          >
            <HeartOff className="w-12 h-12 text-muted-foreground mx-auto mb-3 opacity-50" />
            <p className="text-muted-foreground">No buddies found</p>
            <button onClick={() => { setSearchQuery(""); setActiveFilter("All"); }} className="mt-2 text-accent text-sm hover:underline">
              Clear filters
            </button>
          </motion.div>
        )}

        {activeTab === "experiences" && filteredExperiences.length === 0 && (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="text-center py-12"
          >
            <Calendar className="w-12 h-12 text-muted-foreground mx-auto mb-3 opacity-50" />
            <p className="text-muted-foreground">No experiences found</p>
            <button onClick={() => { setSearchQuery(""); setActiveFilter("All"); }} className="mt-2 text-accent text-sm hover:underline">
              Clear filters
            </button>
          </motion.div>
        )}
      </div>

      {/* Details Modal for Experiences */}
      {showDetailsModal && selectedExperience && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4" onClick={() => setShowDetailsModal(false)}>
          <div className="bg-background border border-border rounded-2xl max-w-md w-full max-h-[90vh] overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="relative">
              <img 
                src={selectedExperience.image} 
                alt={selectedExperience.name}
                className="w-full h-48 object-cover rounded-t-2xl"
                onError={handleImageError}
              />
              <button 
                onClick={() => setShowDetailsModal(false)}
                className="absolute top-2 right-2 p-1.5 bg-black/50 rounded-full text-white hover:bg-black/70 transition-colors"
              >
                <X className="w-4 h-4" />
              </button>
              <div className="absolute bottom-2 left-2 bg-black/60 text-white text-xs px-2 py-0.5 rounded-full">
                {selectedExperience.category}
              </div>
            </div>
            <div className="p-5">
              <h2 className="text-xl font-bold mb-2">{selectedExperience.name}</h2>
              <div className="flex items-center gap-3 mb-3 text-sm">
                <div className="flex items-center gap-1 text-gold">
                  <Star className="w-4 h-4 fill-current" /> {selectedExperience.rating} ({selectedExperience.reviews})
                </div>
                <span className="text-accent font-bold">{selectedExperience.price}</span>
              </div>
              <div className="flex items-center gap-2 text-xs text-muted-foreground mb-3">
                <MapPin className="w-3 h-3" /> {selectedExperience.location}
                <span className="mx-1">•</span>
                <Clock className="w-3 h-3" /> {selectedExperience.duration}
              </div>
              <p className="text-sm text-foreground mb-4">{selectedExperience.description}</p>
              <div className="mb-4">
                <h3 className="font-semibold text-sm mb-2">Highlights</h3>
                <ul className="space-y-1">
                  {selectedExperience.highlights.map((highlight: string, idx: number) => (
                    <li key={idx} className="text-xs text-muted-foreground flex items-center gap-2">
                      <span className="w-1.5 h-1.5 rounded-full bg-accent"></span>
                      {highlight}
                    </li>
                  ))}
                </ul>
              </div>
              <button 
                onClick={() => {
                  handleBookExperience(selectedExperience);
                  setShowDetailsModal(false);
                }}
                className="w-full btn-accent py-2 rounded-lg flex items-center justify-center gap-2"
              >
                <Calendar className="w-4 h-4" /> Book Now
              </button>
            </div>
          </div>
        </div>
      )}

      <Footer />
    </div>
  );
};

export default LocalBuddies;