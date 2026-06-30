import { useState, useEffect } from "react";
import { Search, Filter, Star, MapPin, MessageSquare, Heart, Languages, Trash2, Clock, Calendar, X } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import { useNavigate } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { toast } from "sonner";
import { experiencesApi } from "@/shared/services/api-experiences";
import { profilesApi } from "@/shared/services/api-profiles";
import { authStorage } from "@/shared/lib/auth";
import type {
  ExperienceAvailabilityDto,
  ExperienceReviewDto,
  ExperienceSummaryDto,
  LocalBuddyListItemResponse,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";

const buddyFilters = ["All", "Following", "Free", "Verified", "Top Rated", "Available Now"];
const experienceFilters = ["All", "Favorites", "Free", "Top Rated"];

interface DisplayExperience {
  id: number;
  name: string;
  location: string;
  rating: number;
  reviews: number;
  price: string;
  duration: string;
  image: string;
  category: string;
  description: string;
  highlights: string[];
}

interface DisplayBuddy {
  id: number;
  userId: string;
  name: string;
  location: string;
  rating: number;
  reviews: number;
  price: string;
  languages: string;
  interests: string[];
  verified: boolean;
  photo: string;
  bio: string;
  followersCount: number;
  isFollowing: boolean;
}

const LocalBuddies = () => {
  const navigate = useNavigate();
  const [activeFilter, setActiveFilter] = useState("All");
  const [activeTab, setActiveTab] = useState<"buddies" | "experiences">("buddies");
  const [likedExperiences, setLikedExperiences] = useState<number[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedExperience, setSelectedExperience] = useState<DisplayExperience | null>(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [availability, setAvailability] = useState<ExperienceAvailabilityDto[]>([]);
  const [selectedAvailabilityId, setSelectedAvailabilityId] = useState("");
  const [guestsCount, setGuestsCount] = useState(1);
  const [experienceReviews, setExperienceReviews] = useState<ExperienceReviewDto[]>([]);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewText, setReviewText] = useState("");

  const [apiExperiences, setApiExperiences] = useState<ExperienceSummaryDto[]>([]);
  const [apiBuddies, setApiBuddies] = useState<LocalBuddyListItemResponse[]>([]);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const [region, setRegion] = useState<RegionHierarchyGids>({});

  // Experience favorites do not have a backend contract yet.
  useEffect(() => {
    const savedExperiences = localStorage.getItem("likedExperiences");
    if (savedExperiences) setLikedExperiences(JSON.parse(savedExperiences));
  }, []);

  // Fetch public experiences from the backend.
  useEffect(() => {
    setLoadState({ status: "loading" });
    Promise.all([
      experiencesApi.getExperiences({
        pageSize: 100,
        adm0Gid: region.adm0Gid,
        adm1Gid: region.adm1Gid,
        adm2Gid: region.adm2Gid,
        adm3Gid: region.adm3Gid,
      }),
      profilesApi.getLocalBuddies(),
    ])
      .then(([experiencePage, loadedBuddies]) => {
        setApiExperiences(experiencePage.items);
        setApiBuddies(loadedBuddies);
        setLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        const message = error instanceof Error ? error.message : "Could not load buddies and experiences.";
        setLoadState({ status: "error", message });
        toast.error(message);
      });
  }, [region.adm0Gid, region.adm1Gid, region.adm2Gid, region.adm3Gid]);

  useEffect(() => {
    localStorage.setItem("likedExperiences", JSON.stringify(likedExperiences));
  }, [likedExperiences]);

  const handleBuddyFollow = async (userId: string, name: string) => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in to follow local buddies.");
      navigate("/auth");
      return;
    }
    const buddy = apiBuddies.find((item) => item.userId === userId);
    if (!buddy) return;
    try {
      const status = buddy.isFollowing
        ? await profilesApi.unfollowUser(userId)
        : await profilesApi.followUser(userId);
      setApiBuddies((current) => current.map((item) =>
        item.userId === userId
          ? { ...item, isFollowing: status.isFollowing, followersCount: status.followersCount }
          : item));
      toast.success(status.isFollowing ? `Following ${name}.` : `Unfollowed ${name}.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update follow status.");
    }
  };

  const handleExperienceLike = (id: number, name: string) => {
    const isLiked = likedExperiences.includes(id);
    setLikedExperiences(prev => isLiked ? prev.filter(i => i !== id) : [...prev, id]);
    toast.success(isLiked ? `Removed ${name} from favorites` : `Added ${name} to favorites ❤️`);
  };

  const handleRemoveAllFavorites = async () => {
    if (activeTab === "buddies") {
      const followed = apiBuddies.filter((buddy) => buddy.isFollowing);
      try {
        await Promise.all(followed.map((buddy) => profilesApi.unfollowUser(buddy.userId)));
        setApiBuddies((current) => current.map((buddy) =>
          buddy.isFollowing ? { ...buddy, isFollowing: false, followersCount: Math.max(0, buddy.followersCount - 1) } : buddy));
        toast.success("All visible buddies unfollowed.");
      } catch (error) {
        toast.error(error instanceof Error ? error.message : "Could not unfollow every buddy.");
      }
    } else {
      setLikedExperiences([]);
      toast.success("All experiences removed from favorites");
    }
  };

  // Buddy actions
  const handleConnect = (buddy: DisplayBuddy) => {
    toast.success(`Connecting with ${buddy.name}...`);
    navigate("/messages", { 
      state: { 
        selectedBuddy: {
          userId: buddy.userId,
          name: buddy.name,
          photo: buddy.photo,
          status: "online",
          lastSeen: "Online now"
        }
      } 
    });
  };

  const handleViewProfile = (buddy: DisplayBuddy) => {
    navigate(`/profile/${buddy.userId}`, { state: { buddy } });
  };

  // Experience actions
  const handleViewDetails = async (exp: DisplayExperience) => {
    try {
      const [detail, slots, reviews] = await Promise.all([
        experiencesApi.getExperienceById(exp.id),
        experiencesApi.getAvailability(exp.id),
        experiencesApi.getReviews(exp.id),
      ]);
      setSelectedExperience({
        id: detail.id,
        name: detail.name,
        location: detail.address || "Egypt",
        rating: detail.rating ?? 0,
        reviews: detail.reviews ?? reviews.length,
        price: detail.priceRange || "See available dates",
        duration: detail.currentInsight?.openStatus || "Scheduled experience",
        image: detail.featuredImages[0]?.link || "",
        category: detail.category,
        description: detail.description || "",
        highlights: detail.amenities,
      });
      setAvailability(slots);
      setSelectedAvailabilityId(slots[0]?.id || "");
      setExperienceReviews(reviews);
      setShowDetailsModal(true);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load experience details.");
    }
  };

  const handleBookExperience = async (exp: DisplayExperience) => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in as a traveler to book this experience.");
      return;
    }
    if (!showDetailsModal || selectedExperience?.id !== exp.id) {
      await handleViewDetails(exp);
      return;
    }
    if (!selectedAvailabilityId) {
      toast.error("Select an available date first.");
      return;
    }
    try {
      await experiencesApi.createBooking(exp.id, {
        availabilityId: selectedAvailabilityId,
        guestsCount,
      });
      toast.success(`"${exp.name}" booking requested.`);
      setShowDetailsModal(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not create booking.");
    }
  };

  const submitExperienceReview = async () => {
    if (!selectedExperience || !reviewText.trim()) return;
    try {
      const created = await experiencesApi.createReview(selectedExperience.id, {
        rating: reviewRating,
        reviewText,
      });
      setExperienceReviews((current) => [created, ...current]);
      setReviewText("");
      toast.success("Review submitted.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not submit review.");
    }
  };

  // Fallback sand image (only if other images fail)
  const FALLBACK_SAND_IMAGE = "https://cdn.pixabay.com/photo/2013/07/18/20/26/sand-164878_640.jpg";

  const handleImageError = (e: React.SyntheticEvent<HTMLImageElement, Event>) => {
    e.currentTarget.src = FALLBACK_SAND_IMAGE;
  };

  // Filtering logic
  const allBuddies: DisplayBuddy[] = apiBuddies.map((buddy, index) => ({
    id: index + 1,
    userId: buddy.userId,
    name: buddy.displayName,
    location: buddy.city,
    rating: buddy.rating,
    reviews: buddy.reviewsCount,
    price: "Free",
    languages: buddy.languages || "Not specified",
    interests: buddy.interests.map((interest) => interest.name),
    verified: buddy.verificationStatus === "Approved",
    photo: buddy.profileImageUrl || `https://api.dicebear.com/9.x/initials/svg?seed=${encodeURIComponent(buddy.displayName)}`,
    bio: buddy.bio || "",
    followersCount: buddy.followersCount,
    isFollowing: buddy.isFollowing,
  }));

  const getFilteredBuddies = () => {
    let filtered = allBuddies;
    if (searchQuery) {
      filtered = filtered.filter(buddy => 
        buddy.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        buddy.location.toLowerCase().includes(searchQuery.toLowerCase()) ||
        buddy.interests.some(interest => interest.toLowerCase().includes(searchQuery.toLowerCase()))
      );
    }
    switch (activeFilter) {
      case "Following": filtered = filtered.filter(buddy => buddy.isFollowing); break;
      case "Free": filtered = filtered.filter(buddy => buddy.price === "Free"); break;
      case "Verified": filtered = filtered.filter(buddy => buddy.verified); break;
      case "Top Rated": filtered = filtered.filter(buddy => buddy.rating >= 4.8); break;
      case "Available Now": filtered = filtered.filter(buddy => buddy.verified); break;
      default: break;
    }
    return filtered;
  };

  const allExperiences: DisplayExperience[] = apiExperiences.map((exp) => ({
    id: exp.id,
    name: exp.name,
    location: exp.address || "Egypt",
    rating: exp.rating ?? 0,
    reviews: exp.reviews ?? exp.featuredReviews.length,
    price: exp.priceRange || "See dates",
    duration: exp.currentInsight?.openStatus || "Scheduled",
    image: exp.featuredImages[0]?.link || "",
    category: exp.category,
    description: exp.description || "",
    highlights: exp.amenities,
  }));

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
  const favoriteCount = activeTab === "buddies"
    ? allBuddies.filter((buddy) => buddy.isFollowing).length
    : likedExperiences.length;
  const filters = activeTab === "buddies" ? buddyFilters : experienceFilters;

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
              <span className="text-sm font-medium">
                {favoriteCount} {activeTab === "buddies"
                  ? "Following"
                  : `Favorite${favoriteCount !== 1 ? "s" : ""}`}
              </span>
              <button
                onClick={() => void handleRemoveAllFavorites()}
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
              {(f === "Favorites" || f === "Following") && <Heart className="w-3 h-3" />}
              {f}
              {(f === "Favorites" || f === "Following") && favoriteCount > 0 && (
                <span className="ml-1 px-1.5 py-0.5 bg-primary-foreground/20 rounded-full text-[10px]">
                  {favoriteCount}
                </span>
              )}
            </button>
          ))}
        </div>

        {activeTab === "experiences" && (
          <div className="card-glass mb-6 p-4">
            <RegionCascadeSelect
              value={region}
              onChange={setRegion}
              label="Filter experiences by backend region"
            />
          </div>
        )}

        {/* Tabs */}
        <div className="flex rounded-lg bg-secondary p-1 mb-8 max-w-xs">
          <button
            onClick={() => { setActiveTab("buddies"); setActiveFilter("All"); }}
            className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 ${
              activeTab === "buddies" ? "bg-primary text-primary-foreground" : "text-muted-foreground"
            }`}
          >
            👥 Local Buddies
          </button>
          <button
            onClick={() => { setActiveTab("experiences"); setActiveFilter("All"); }}
            className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 ${
              activeTab === "experiences" ? "bg-primary text-primary-foreground" : "text-muted-foreground"
            }`}
          >
            🎯 Experiences
          </button>
        </div>

        {/* Results count */}
        {loadState.status === "loading" && (
          <div className="card-glass mb-6 p-8 text-center text-sm text-muted-foreground">
            Loading buddies and experiences…
          </div>
        )}
        {loadState.status === "error" && (
          <div className="mb-6 rounded-xl border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
            {loadState.message}
          </div>
        )}
        {activeTab === "buddies" && (
          <p className="text-xs text-muted-foreground mb-4">
            Showing {filteredBuddies.length} of {allBuddies.length} buddies
          </p>
        )}
        {activeTab === "experiences" && (
          <p className="text-xs text-muted-foreground mb-4">
            Showing {filteredExperiences.length} of {allExperiences.length} experiences
          </p>
        )}
        {loadState.status === "ready" && activeTab === "buddies" && filteredBuddies.length === 0 && (
          <div className="card-glass mb-6 p-8 text-center text-sm text-muted-foreground">
            No local buddies match the current filters.
          </div>
        )}
        {loadState.status === "ready" && activeTab === "experiences" && filteredExperiences.length === 0 && (
          <div className="card-glass mb-6 p-8 text-center text-sm text-muted-foreground">
            No experiences match the current filters.
          </div>
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
                      aria-label={b.isFollowing ? `Unfollow ${b.name}` : `Follow ${b.name}`}
                      aria-pressed={b.isFollowing}
                      onClick={() => void handleBuddyFollow(b.userId, b.name)}
                      className="focus:outline-none group relative"
                    >
                      <Heart 
                        className={`w-5 h-5 transition-all duration-300 ${
                          b.isFollowing
                            ? "fill-red-500 text-red-500" 
                            : "text-muted-foreground group-hover:text-red-500"
                        }`}
                      />
                      {b.isFollowing && (
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
                    <span className="text-muted-foreground">{b.followersCount} followers</span>
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

                  {b.isFollowing && (
                    <motion.div 
                      initial={{ opacity: 0, x: -10 }}
                      animate={{ opacity: 1, x: 0 }}
                      className="absolute top-3 right-12"
                    >
                      <div className="bg-red-500/20 text-red-500 text-[10px] px-1.5 py-0.5 rounded-full flex items-center gap-1">
                        <Heart className="w-2 h-2 fill-red-500" />
                        Following
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
                          handleExperienceLike(exp.id, exp.name);
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

              <div className="mb-4">
                <h3 className="mb-2 text-sm font-semibold">Available dates</h3>
                {availability.length === 0 ? (
                  <p className="text-xs text-muted-foreground">No bookable dates are currently available.</p>
                ) : (
                  <div className="grid grid-cols-[1fr_auto] gap-2">
                    <select
                      value={selectedAvailabilityId}
                      onChange={(event) => setSelectedAvailabilityId(event.target.value)}
                      className="rounded-lg border border-border bg-secondary px-2 py-2 text-xs"
                    >
                      {availability.map((slot) => (
                        <option key={slot.id} value={slot.id}>
                          {new Date(slot.startTimeUtc).toLocaleString()} · ${slot.pricePerPerson} · {slot.remainingCapacity} left
                        </option>
                      ))}
                    </select>
                    <input
                      type="number"
                      min="1"
                      max={availability.find((slot) => slot.id === selectedAvailabilityId)?.remainingCapacity ?? 1}
                      value={guestsCount}
                      onChange={(event) => setGuestsCount(Number(event.target.value))}
                      className="w-20 rounded-lg border border-border bg-secondary px-2 text-xs"
                      aria-label="Guests"
                    />
                  </div>
                )}
              </div>

              <div className="mb-4 border-t border-border pt-4">
                <h3 className="mb-2 text-sm font-semibold">Traveler reviews</h3>
                <div className="max-h-28 space-y-2 overflow-y-auto">
                  {experienceReviews.length === 0 && <p className="text-xs text-muted-foreground">No reviews yet.</p>}
                  {experienceReviews.map((review) => (
                    <div key={review.id} className="rounded-lg bg-secondary/40 p-2 text-xs">
                      <p className="font-medium">{review.reviewerName || "Traveler"} · {review.rating ?? "—"}/5</p>
                      <p className="text-muted-foreground">{review.reviewText}</p>
                    </div>
                  ))}
                </div>
                {authStorage.hasAnyRole(["Traveler"]) && (
                  <div className="mt-2 flex gap-2">
                    <select value={reviewRating} onChange={(event) => setReviewRating(Number(event.target.value))} className="rounded bg-secondary px-2 text-xs">
                      {[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value}/5</option>)}
                    </select>
                    <input value={reviewText} onChange={(event) => setReviewText(event.target.value)} className="input-glass min-w-0 flex-1" placeholder="Write a review" />
                    <button onClick={() => void submitExperienceReview()} className="rounded bg-secondary px-3 text-xs">Post</button>
                  </div>
                )}
              </div>
              <button 
                onClick={() => void handleBookExperience(selectedExperience)}
                disabled={!selectedAvailabilityId}
                className="w-full btn-accent py-2 rounded-lg flex items-center justify-center gap-2 disabled:cursor-not-allowed disabled:opacity-50"
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
