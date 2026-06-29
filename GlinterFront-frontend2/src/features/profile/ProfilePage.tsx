import { useState, useEffect } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import { Star, MapPin, MessageSquare, Heart, Languages, ArrowLeft, Share2, CheckCircle } from "lucide-react";
import { motion } from "framer-motion";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { toast } from "sonner";

const ProfilePage = () => {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const [buddy, setBuddy] = useState<any>(null);
  const [isLiked, setIsLiked] = useState(false);

  useEffect(() => {
    const buddyFromState = location.state?.buddy;
    if (buddyFromState) {
      setBuddy(buddyFromState);
      const savedLikes = localStorage.getItem("likedBuddies");
      if (savedLikes) {
        const likes = JSON.parse(savedLikes);
        setIsLiked(likes.includes(buddyFromState.id));
      }
    }
  }, [id, location.state]);

  const handleLike = () => {
    const savedLikes = localStorage.getItem("likedBuddies");
    let likes = savedLikes ? JSON.parse(savedLikes) : [];

    if (isLiked) {
      likes = likes.filter((likedId: number) => likedId !== buddy.id);
      toast.info(`Removed ${buddy.name} from favorites`);
    } else {
      likes.push(buddy.id);
      toast.success(`Added ${buddy.name} to favorites! ❤️`);
    }

    localStorage.setItem("likedBuddies", JSON.stringify(likes));
    setIsLiked(!isLiked);
  };

  const handleConnect = () => {
    localStorage.setItem("selectedBuddy", JSON.stringify({
      id: buddy.name,
      name: buddy.name,
      photo: buddy.photo,
      status: "online",
    }));

    toast.success(`Connecting with ${buddy.name}...`);
    navigate("/messages", {
      state: {
        selectedBuddy: {
          name: buddy.name,
          photo: buddy.photo,
          status: "online",
        },
      },
    });
  };

  if (!buddy) {
    return (
      <div className="min-h-screen bg-background">
        <Navbar />
        <div className="container mx-auto px-4 py-8 text-center">
          <p>Loading...</p>
        </div>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <button
          onClick={() => navigate(-1)}
          className="flex items-center gap-2 text-muted-foreground hover:text-foreground transition-colors mb-6"
        >
          <ArrowLeft className="w-4 h-4" /> Back
        </button>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="card-glass p-6 mb-6"
        >
          <div className="flex flex-col md:flex-row gap-6">
            <div className="relative">
              <img
                src={buddy.photo}
                alt={buddy.name}
                className="w-32 h-32 md:w-40 md:h-40 rounded-full object-cover"
              />
              {buddy.verified && (
                <div className="absolute bottom-2 right-2 w-6 h-6 rounded-full bg-green-500 border-2 border-card flex items-center justify-center">
                  <CheckCircle className="w-4 h-4 text-white" />
                </div>
              )}
            </div>

            <div className="flex-1">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <h1 className="text-2xl md:text-3xl font-bold mb-2">{buddy.name}</h1>
                  <div className="flex items-center gap-4 text-sm text-muted-foreground mb-3">
                    <span className="flex items-center gap-1">
                      <MapPin className="w-4 h-4" /> {buddy.location}
                    </span>
                    <span className="flex items-center gap-1 text-gold">
                      <Star className="w-4 h-4 fill-current" /> {buddy.rating} ({buddy.reviews} reviews)
                    </span>
                  </div>
                  <div className="flex flex-wrap gap-2 mb-4">
                    {buddy.interests.map((interest: string) => (
                      <span key={interest} className="text-xs bg-secondary px-2 py-1 rounded-full">
                        {interest}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="flex gap-2">
                  <button
                    onClick={handleLike}
                    className="p-2 rounded-lg border border-border hover:bg-secondary transition-colors"
                  >
                    <Heart className={`w-5 h-5 ${isLiked ? "fill-red-500 text-red-500" : "text-muted-foreground"}`} />
                  </button>
                  <button
                    onClick={() => {
                      navigator.clipboard.writeText(window.location.href);
                      toast.success("Profile link copied!");
                    }}
                    className="p-2 rounded-lg border border-border hover:bg-secondary transition-colors"
                  >
                    <Share2 className="w-5 h-5 text-muted-foreground" />
                  </button>
                </div>
              </div>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6 pt-4 border-t border-border">
                <div className="text-center">
                  <p className="text-2xl font-bold">{buddy.price === "Free" ? "Free" : buddy.price}</p>
                  <p className="text-xs text-muted-foreground">Price</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold">{buddy.languages.split(",").length}</p>
                  <p className="text-xs text-muted-foreground">Languages</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold">{buddy.reviews}</p>
                  <p className="text-xs text-muted-foreground">Reviews</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold">✓</p>
                  <p className="text-xs text-muted-foreground">Verified</p>
                </div>
              </div>
            </div>
          </div>
        </motion.div>

        <div className="card-glass p-6 mb-6">
          <h2 className="font-bold mb-3">About {buddy.name}</h2>
          <p className="text-sm text-muted-foreground leading-relaxed">{buddy.bio}</p>
        </div>

        <div className="card-glass p-6 mb-6">
          <h2 className="font-bold mb-3 flex items-center gap-2">
            <Languages className="w-4 h-4" /> Languages
          </h2>
          <div className="flex flex-wrap gap-2">
            {buddy.languages.split(", ").map((lang: string) => (
              <span key={lang} className="px-3 py-1 bg-secondary rounded-full text-sm">
                {lang}
              </span>
            ))}
          </div>
        </div>

        <div className="card-glass p-6">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <p className="text-sm text-muted-foreground">Price</p>
              {buddy.price === "Free" ? (
                <p className="text-2xl font-bold text-green-400">Free</p>
              ) : (
                <p className="text-2xl font-bold text-accent">{buddy.price}</p>
              )}
            </div>
            <button
              onClick={handleConnect}
              className="btn-accent px-6 py-2 rounded-lg flex items-center gap-2"
            >
              <MessageSquare className="w-4 h-4" /> Connect with {buddy.name.split(" ")[0]}
            </button>
          </div>
        </div>
      </div>
      <Footer />
    </div>
  );
};

export default ProfilePage;
