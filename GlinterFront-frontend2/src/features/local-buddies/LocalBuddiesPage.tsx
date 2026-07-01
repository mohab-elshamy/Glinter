import { useState, useEffect } from "react";
import { Search, Filter, Star, MapPin, MessageSquare, Heart, Languages, Trash2, Clock, Calendar, Images, Users } from "lucide-react";
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
  ExperienceMapItemDto,
  ExperienceReviewDto,
  ExperienceResponseDto,
  ExperienceSortBy,
  ExperienceSummaryDto,
  ExperienceVisitInsightDto,
  LocalBuddyListItemResponse,
  SortDirection,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import LeafletMap, { type MapMarker } from "@/components/LeafletMap";
import { formatExperiencePrice, isFreeExperience } from "@/shared/lib/price";
import ExperienceDetailsModal from "./ExperienceDetailsModal";
import cairoImage from "@/assets/cairo.jpg";
import { createInitialsAvatar } from "@/shared/lib/avatar";

const buddyFilters = ["All", "Following", "Free", "Verified", "Top Rated", "Available Now"];
const experienceFilters = ["All", "Favorites", "Free", "Top Rated"];
type ExperienceSortSelection = `${ExperienceSortBy}:${SortDirection}`;

interface DisplayExperience {
  id: number;
  name: string;
  location: string;
  rating: number;
  reviews: number;
  startingPricePerPerson?: number;
  priceRange?: string;
  openStatus: string;
  image: string;
  category: string;
  description: string;
  highlights: string[];
  imageCount: number;
  crowdLevel?: string;
  popularityPercentage?: number;
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

const formatOpenStatus = (insight?: ExperienceVisitInsightDto) => {
  if (!insight || insight.openStatus === "unknown") return "Hours unknown";
  return insight.openStatus === "open" ? "Open now" : "Closed now";
};

const minimumSlotPrice = (slots: ExperienceAvailabilityDto[]) => {
  const prices = slots
    .filter((slot) => slot.isActive)
    .map((slot) => slot.pricePerPerson);
  return prices.length > 0 ? Math.min(...prices) : undefined;
};

const LocalBuddies = () => {
  const navigate = useNavigate();
  const [activeFilter, setActiveFilter] = useState("All");
  const [activeTab, setActiveTab] = useState<"buddies" | "experiences">("buddies");
  const [likedExperiences, setLikedExperiences] = useState<number[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedExperience, setSelectedExperience] = useState<ExperienceResponseDto | null>(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [availability, setAvailability] = useState<ExperienceAvailabilityDto[]>([]);
  const [experienceReviews, setExperienceReviews] = useState<ExperienceReviewDto[]>([]);
  const [visitInsight, setVisitInsight] = useState<ExperienceVisitInsightDto>();

  const [apiExperiences, setApiExperiences] = useState<ExperienceSummaryDto[]>([]);
  const [mapExperiences, setMapExperiences] = useState<ExperienceMapItemDto[]>([]);
  const [apiBuddies, setApiBuddies] = useState<LocalBuddyListItemResponse[]>([]);
  const [buddyLoadState, setBuddyLoadState] = useState<LoadState>({ status: "loading" });
  const [experienceLoadState, setExperienceLoadState] = useState<LoadState>({ status: "loading" });
  const [region, setRegion] = useState<RegionHierarchyGids>({});
  const [experiencePage, setExperiencePage] = useState(1);
  const [experienceTotal, setExperienceTotal] = useState(0);
  const [appliedExperienceSearch, setAppliedExperienceSearch] = useState("");
  const [experienceSortBy, setExperienceSortBy] = useState<ExperienceSortBy>("Recommended");
  const [experienceSortDirection, setExperienceSortDirection] = useState<SortDirection>("Desc");
  const [sortPosition, setSortPosition] = useState<{ latitude: number; longitude: number }>();
  const [locatingForSort, setLocatingForSort] = useState(false);
  const experiencePageSize = 12;
  const loadState = activeTab === "buddies" ? buddyLoadState : experienceLoadState;

  useEffect(() => {
    if (!authStorage.isAuthenticated()) {
      setLikedExperiences([]);
      return;
    }
    profilesApi.getExperienceFavoriteIds()
      .then(setLikedExperiences)
      .catch((error: unknown) => {
        toast.error(error instanceof Error ? error.message : "Could not load experience favorites.");
      });
  }, []);

  useEffect(() => {
    profilesApi.getLocalBuddies()
      .then((loadedBuddies) => {
        setApiBuddies(loadedBuddies);
        setBuddyLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        const errorText = error instanceof Error ? error.message : "Could not load local buddies.";
        setBuddyLoadState({ status: "error", message: errorText });
      });
  }, []);

  useEffect(() => {
    if (activeTab !== "experiences") return;
    const timer = window.setTimeout(() => {
      setAppliedExperienceSearch(searchQuery.trim());
      setExperiencePage(1);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [activeTab, searchQuery]);

  // Fetch public experiences and map markers from the backend.
  useEffect(() => {
    if (activeTab !== "experiences") return;
    setExperienceLoadState({ status: "loading" });
    const filters = {
      page: experiencePage,
      pageSize: experiencePageSize,
      search: appliedExperienceSearch || undefined,
      minRating: activeFilter === "Top Rated" ? 4.8 : undefined,
      isFree: activeFilter === "Free" ? true : undefined,
      adm0Gid: region.adm0Gid,
      adm1Gid: region.adm1Gid,
      adm2Gid: region.adm2Gid,
      adm3Gid: region.adm3Gid,
      sortBy: experienceSortBy,
      sortDirection: experienceSortDirection,
      currentLatitude: experienceSortBy === "Distance" ? sortPosition?.latitude : undefined,
      currentLongitude: experienceSortBy === "Distance" ? sortPosition?.longitude : undefined,
    };

    if (activeFilter === "Favorites") {
      Promise.all(likedExperiences.map((id) =>
        experiencesApi.getExperienceById(id).catch(() => undefined)))
        .then((results) => {
          const search = appliedExperienceSearch.toLowerCase();
          const filtered = results
            .filter((item): item is ExperienceResponseDto => item != null && item.isActive)
            .filter((item) =>
              (!search ||
                item.name.toLowerCase().includes(search) ||
                (item.address ?? "").toLowerCase().includes(search) ||
                item.category.toLowerCase().includes(search)) &&
              (!region.adm0Gid || item.adm0Gid === region.adm0Gid) &&
              (!region.adm1Gid || item.adm1Gid === region.adm1Gid) &&
              (!region.adm2Gid || item.adm2Gid === region.adm2Gid) &&
              (!region.adm3Gid || item.adm3Gid === region.adm3Gid));
          const direction = experienceSortDirection === "Asc" ? 1 : -1;
          const distance = (item: ExperienceResponseDto) => {
            if (!sortPosition || item.latitude == null || item.longitude == null) {
              return Number.POSITIVE_INFINITY;
            }
            return Math.hypot(
              item.latitude - sortPosition.latitude,
              item.longitude - sortPosition.longitude,
            );
          };
          const value = (item: ExperienceResponseDto): number | string => {
            switch (experienceSortBy) {
              case "Price":
                return item.startingPricePerPerson
                  ?? (experienceSortDirection === "Asc"
                    ? Number.POSITIVE_INFINITY
                    : Number.NEGATIVE_INFINITY);
              case "Rating": return item.rating ?? -1;
              case "Reviews": return item.reviews ?? 0;
              case "Name": return item.name.toLowerCase();
              case "Newest": return Date.parse(item.createdAtUtc);
              case "Popularity": return item.currentInsight?.popularityPercentage ?? -1;
              case "OpenNow": return item.currentInsight?.isOpen ? 1 : 0;
              case "Distance": return distance(item);
              default: return 0;
            }
          };
          filtered.sort((left, right) => {
            const leftValue = value(left);
            const rightValue = value(right);
            if (typeof leftValue === "string" && typeof rightValue === "string") {
              return leftValue.localeCompare(rightValue) * direction;
            }
            return ((leftValue as number) - (rightValue as number)) * direction;
          });
          const start = (experiencePage - 1) * experiencePageSize;
          setApiExperiences(filtered.slice(start, start + experiencePageSize));
          setMapExperiences(filtered.flatMap((item) =>
            item.latitude == null || item.longitude == null
              ? []
              : [{
                  id: item.id,
                  category: item.category,
                  sourceType: item.sourceType,
                  name: item.name,
                  address: item.address,
                  adm0Gid: item.adm0Gid,
                  adm1Gid: item.adm1Gid,
                  adm2Gid: item.adm2Gid,
                  adm3Gid: item.adm3Gid,
                  latitude: item.latitude,
                  longitude: item.longitude,
                  rating: item.rating,
                  reviews: item.reviews,
                  startingPricePerPerson: item.startingPricePerPerson,
                  primaryImage: item.featuredImages[0]?.link,
                  isOpenNow: item.currentInsight?.isOpen,
                  popularityPercentageNow: item.currentInsight?.popularityPercentage,
                }]));
          setExperienceTotal(filtered.length);
          const totalPages = Math.max(1, Math.ceil(filtered.length / experiencePageSize));
          if (experiencePage > totalPages) setExperiencePage(totalPages);
          setExperienceLoadState({ status: "ready" });
        })
        .catch((error: unknown) => {
          const errorText = error instanceof Error ? error.message : "Could not load favorite experiences.";
          setExperienceLoadState({ status: "error", message: errorText });
        });
      return;
    }

    Promise.all([
      experiencesApi.getExperiences(filters),
      experiencesApi.getMapExperiences(filters),
    ])
      .then(([pageResult, mapItems]) => {
        setApiExperiences(pageResult.items);
        setMapExperiences(mapItems);
        setExperienceTotal(pageResult.totalCount);
        setExperienceLoadState({ status: "ready" });
      })
      .catch((error: unknown) => {
        const errorText = error instanceof Error ? error.message : "Could not load experiences.";
        setExperienceLoadState({ status: "error", message: errorText });
        toast.error(errorText);
      });
  }, [
    activeTab,
    activeFilter,
    likedExperiences,
    appliedExperienceSearch,
    experiencePage,
    experienceSortBy,
    experienceSortDirection,
    sortPosition,
    region.adm0Gid,
    region.adm1Gid,
    region.adm2Gid,
    region.adm3Gid,
  ]);

  const changeExperienceSorting = (selection: ExperienceSortSelection) => {
    const [nextSortBy, nextDirection] = selection.split(":") as [
      ExperienceSortBy,
      SortDirection,
    ];

    const apply = (position = sortPosition) => {
      setExperienceSortBy(nextSortBy);
      setExperienceSortDirection(nextDirection);
      if (position) setSortPosition(position);
      setExperiencePage(1);
    };

    if (nextSortBy !== "Distance" || sortPosition) {
      apply();
      return;
    }

    if (!navigator.geolocation) {
      toast.error("Distance sorting is not supported by this browser.");
      return;
    }

    setLocatingForSort(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        apply({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        });
        setLocatingForSort(false);
      },
      () => {
        setLocatingForSort(false);
        toast.error("Allow location access to sort experiences by distance.");
      },
      { enableHighAccuracy: true, timeout: 10_000, maximumAge: 300_000 },
    );
  };

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

  const handleExperienceLike = async (id: number, name: string) => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in to save experience favorites.");
      navigate("/auth");
      return;
    }
    const isLiked = likedExperiences.includes(id);
    try {
      const status = isLiked
        ? await profilesApi.removeExperienceFavorite(id)
        : await profilesApi.addExperienceFavorite(id);
      setLikedExperiences((current) => status.isFavorite
        ? [...new Set([...current, id])]
        : current.filter((item) => item !== id));
      toast.success(status.isFavorite
        ? `Added ${name} to favorites.`
        : `Removed ${name} from favorites.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not update experience favorites.");
    }
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
      try {
        await profilesApi.clearExperienceFavorites();
        setLikedExperiences([]);
        toast.success("All experience favorites removed.");
      } catch (error) {
        toast.error(error instanceof Error ? error.message : "Could not clear experience favorites.");
      }
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
      const [detail, slots, reviews, insight] = await Promise.all([
        experiencesApi.getExperienceById(exp.id),
        experiencesApi.getAvailability(exp.id),
        experiencesApi.getReviews(exp.id, 1, 5),
        experiencesApi.getVisitInsights(exp.id),
      ]);
      setSelectedExperience({
        ...detail,
        startingPricePerPerson: minimumSlotPrice(slots) ?? detail.startingPricePerPerson,
      });
      setAvailability(slots);
      setExperienceReviews(reviews);
      setVisitInsight(insight);
      setShowDetailsModal(true);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load experience details.");
    }
  };

  const handleMapMarker = async (marker: MapMarker) => {
    if (marker.id == null) return;
    const listedDto = apiExperiences.find((experience) => experience.id === marker.id);
    const listed = listedDto
      ? {
          id: listedDto.id,
          name: listedDto.name,
          location: listedDto.address || "Egypt",
          rating: listedDto.rating ?? 0,
          reviews: listedDto.reviews ?? listedDto.featuredReviews.length,
          startingPricePerPerson: listedDto.startingPricePerPerson,
          priceRange: listedDto.priceRange,
          openStatus: formatOpenStatus(listedDto.currentInsight),
          image: listedDto.featuredImages[0]?.link || "",
          category: listedDto.category,
          description: listedDto.description || "",
          highlights: listedDto.amenities,
          imageCount: listedDto.featuredImages.length,
          crowdLevel: listedDto.currentInsight?.crowdLevel === "unknown"
            ? undefined
            : listedDto.currentInsight?.crowdLevel,
          popularityPercentage: listedDto.currentInsight?.popularityPercentage,
        }
      : undefined;
    if (listed) {
      await handleViewDetails(listed);
      return;
    }
    try {
      const detail = await experiencesApi.getExperienceById(marker.id);
      await handleViewDetails({
        id: detail.id,
        name: detail.name,
        location: detail.address || "Egypt",
        rating: detail.rating ?? 0,
        reviews: detail.reviews ?? 0,
        startingPricePerPerson: detail.startingPricePerPerson,
        priceRange: detail.priceRange,
        openStatus: formatOpenStatus(detail.currentInsight),
        image: detail.featuredImages[0]?.link || "",
        category: detail.category,
        description: detail.description || "",
        highlights: detail.amenities,
        imageCount: detail.featuredImages.length,
        crowdLevel: detail.currentInsight?.crowdLevel === "unknown"
          ? undefined
          : detail.currentInsight?.crowdLevel,
        popularityPercentage: detail.currentInsight?.popularityPercentage,
      });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load the mapped experience.");
    }
  };

  // Fallback sand image (only if other images fail)
  const FALLBACK_SAND_IMAGE = cairoImage;

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
    photo: buddy.profileImageUrl || createInitialsAvatar(buddy.displayName),
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
    startingPricePerPerson: exp.startingPricePerPerson,
    priceRange: exp.priceRange,
    openStatus: formatOpenStatus(exp.currentInsight),
    image: exp.featuredImages[0]?.link || "",
    category: exp.category,
    description: exp.description || "",
    highlights: exp.amenities,
    imageCount: exp.featuredImages.length,
    crowdLevel: exp.currentInsight?.crowdLevel === "unknown"
      ? undefined
      : exp.currentInsight?.crowdLevel,
    popularityPercentage: exp.currentInsight?.popularityPercentage,
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
      case "Free":
        filtered = filtered.filter((exp) =>
          isFreeExperience(exp.startingPricePerPerson, exp.priceRange));
        break;
      case "Top Rated": filtered = filtered.filter(exp => exp.rating >= 4.8); break;
      default: break;
    }
    return filtered;
  };

  const filteredBuddies = getFilteredBuddies();
  const filteredExperiences = getFilteredExperiences();
  const experienceMarkers: MapMarker[] = mapExperiences.flatMap((experience) =>
    experience.latitude == null || experience.longitude == null
      ? []
      : [{
          id: experience.id,
          lat: experience.latitude,
          lng: experience.longitude,
          name: experience.name,
          cheapestPrice: experience.startingPricePerPerson,
          data: {
            rating: experience.rating ?? 0,
            area: `${experience.isOpenNow === true ? "Open" : experience.isOpenNow === false ? "Closed" : "Hours unknown"}${experience.popularityPercentageNow == null ? "" : ` · ${experience.popularityPercentageNow}% busy`}`,
          },
        }],
  );
  const experienceTotalPages = Math.max(1, Math.ceil(experienceTotal / experiencePageSize));
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
              onClick={() => {
                setActiveFilter(f);
                if (activeTab === "experiences") setExperiencePage(1);
              }}
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
            <div className="mb-4 flex items-center justify-end">
              <label className="flex items-center gap-2 text-xs text-muted-foreground">
                <span>Sort experiences</span>
                <select
                  value={`${experienceSortBy}:${experienceSortDirection}`}
                  disabled={locatingForSort}
                  onChange={(event) =>
                    changeExperienceSorting(event.target.value as ExperienceSortSelection)}
                  className="rounded-lg border border-border bg-background px-3 py-2 text-xs text-foreground disabled:opacity-50"
                >
                  <option value="Recommended:Desc">Recommended</option>
                  <option value="Price:Asc">Price: low to high</option>
                  <option value="Price:Desc">Price: high to low</option>
                  <option value="Rating:Desc">Highest rated</option>
                  <option value="Reviews:Desc">Most reviewed</option>
                  <option value="Name:Asc">Name: A–Z</option>
                  <option value="Name:Desc">Name: Z–A</option>
                  <option value="Newest:Desc">Newest first</option>
                  <option value="Newest:Asc">Oldest first</option>
                  <option value="Popularity:Desc">Most popular now</option>
                  <option value="OpenNow:Desc">Open now first</option>
                  <option value="Distance:Asc">Nearest to me</option>
                </select>
              </label>
              {locatingForSort && (
                <span className="ml-2 text-xs text-muted-foreground">Finding your location…</span>
              )}
            </div>
            <RegionCascadeSelect
              value={region}
              onChange={(selection) => {
                setRegion(selection);
                setExperiencePage(1);
              }}
              label="Filter experiences by backend region"
            />
            <div className="mt-4 overflow-hidden rounded-xl border border-border">
              <LeafletMap
                center={[26.8206, 30.8025]}
                zoom={6}
                markers={experienceMarkers}
                onMarkerClick={(marker) => void handleMapMarker(marker)}
                showSearch
                showLegend={false}
                height="360px"
              />
            </div>
            {experienceLoadState.status === "ready" && experienceMarkers.length === 0 && (
              <p className="mt-3 text-xs text-muted-foreground">
                No mapped experiences match the current search and region filters.
              </p>
            )}
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
            {activeTab === "experiences" ? "Loading experiences and map…" : "Loading local buddies…"}
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
            Showing {filteredExperiences.length} of {experienceTotal} experiences · page {experiencePage} of {experienceTotalPages}
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
                    {exp.imageCount > 1 && (
                      <div className="absolute bottom-2 right-2 flex items-center gap-1 rounded-full bg-black/60 px-2 py-1 text-[10px] text-white">
                        <Images className="h-3 w-3" /> {exp.imageCount}
                      </div>
                    )}
                  </div>
                  
                  <div className="p-5">
                    <h3 className="font-bold text-base mb-2">{exp.name}</h3>
                    
                    <div className="flex items-center gap-2 text-xs text-muted-foreground mb-2">
                      <MapPin className="w-3 h-3" />
                      <span>{exp.location}</span>
                      <span className="mx-1">•</span>
                      <Clock className="w-3 h-3" />
                      <span>{exp.openStatus}</span>
                    </div>

                    <div className="flex items-center gap-3 mb-3">
                      <span className="flex items-center gap-1 text-gold text-xs">
                        <Star className="w-3 h-3 fill-current" /> {exp.rating} ({exp.reviews})
                      </span>
                      <span className="text-accent font-bold">
                        {formatExperiencePrice(exp.startingPricePerPerson, exp.priceRange)}
                      </span>
                    </div>

                    {(exp.crowdLevel || exp.popularityPercentage != null) && (
                      <div className="mb-3 flex items-center gap-1.5 text-xs text-muted-foreground">
                        <Users className="h-3.5 w-3.5" />
                        <span className="capitalize">{exp.crowdLevel || "Crowd insight"}</span>
                        {exp.popularityPercentage != null && <span>· {exp.popularityPercentage}% busy</span>}
                      </div>
                    )}

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
                        onClick={() => void handleViewDetails(exp)}
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
        {activeTab === "experiences" && experienceTotalPages > 1 && (
          <nav aria-label="Experience pages" className="mt-7 flex items-center justify-center gap-3">
            <button
              type="button"
              disabled={experiencePage <= 1}
              onClick={() => setExperiencePage((value) => Math.max(1, value - 1))}
              className="rounded-lg border border-border px-4 py-2 text-xs disabled:opacity-40"
            >
              Previous
            </button>
            <span className="text-xs text-muted-foreground">
              Page {experiencePage} of {experienceTotalPages}
            </span>
            <button
              type="button"
              disabled={experiencePage >= experienceTotalPages}
              onClick={() => setExperiencePage((value) => Math.min(experienceTotalPages, value + 1))}
              className="rounded-lg border border-border px-4 py-2 text-xs disabled:opacity-40"
            >
              Next
            </button>
          </nav>
        )}

      </div>

      {showDetailsModal && selectedExperience && (
        <ExperienceDetailsModal
          experience={selectedExperience}
          availability={availability}
          initialReviews={experienceReviews}
          initialInsight={visitInsight}
          fallbackImage={FALLBACK_SAND_IMAGE}
          onClose={() => setShowDetailsModal(false)}
        />
      )}

      <Footer />
    </div>
  );
};

export default LocalBuddies;
