import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { Camera, Check, Save } from "lucide-react";
import { toast } from "sonner";
import { profilesApi } from "@/shared/services/api-profiles";
import { authStorage } from "@/shared/lib/auth";

const interestOptions = [
  "Cultural", "Adventure", "Foodie", "Nightlife", "Nature", "Shopping", "Family", "Hidden Gems", "Luxury"
];
const budgetOptions = ["Budget", "Mid-range", "Luxury"];
const travelStyleOptions = ["Solo", "Couple", "Family", "Group", "Business"];

const EditProfileTab = () => {
  const [displayName, setDisplayName] = useState(() => localStorage.getItem("profile_displayName") || "");
  const [bio, setBio] = useState(() => localStorage.getItem("profile_bio") || "");
  const [nationality, setNationality] = useState(() => localStorage.getItem("profile_nationality") || "");
  const [preferredBudgetLevel, setPreferredBudgetLevel] = useState(() => localStorage.getItem("profile_budgetLevel") || "Mid-range");
  const [travelStyle, setTravelStyle] = useState(() => localStorage.getItem("profile_travelStyle") || "Solo");
  const [profileImageUrl, setProfileImageUrl] = useState(() => localStorage.getItem("profile_imageUrl") || "https://images.pexels.com/photos/2379005/pexels-photo-2379005.jpeg?w=150&h=150&fit=crop");
  const [selectedInterests, setSelectedInterests] = useState<string[]>(() => {
    const saved = localStorage.getItem("profile_interests");
    return saved ? JSON.parse(saved) : [];
  });

  useEffect(() => {
    localStorage.setItem("profile_displayName", displayName);
    localStorage.setItem("profile_bio", bio);
    localStorage.setItem("profile_nationality", nationality);
    localStorage.setItem("profile_budgetLevel", preferredBudgetLevel);
    localStorage.setItem("profile_travelStyle", travelStyle);
    localStorage.setItem("profile_imageUrl", profileImageUrl);
    localStorage.setItem("profile_interests", JSON.stringify(selectedInterests));
  }, [displayName, bio, nationality, preferredBudgetLevel, travelStyle, profileImageUrl, selectedInterests]);

  // Fetch profile from backend on mount to pre-fill form
  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;
    profilesApi.getMyProfile()
      .then((data) => {
        if ("nationality" in data) {
          setDisplayName(data.displayName || "");
          setBio(data.bio || "");
          setNationality(data.nationality || "");
          setPreferredBudgetLevel(data.preferredBudgetLevel || "Mid-range");
          setTravelStyle(data.travelStyle || "Solo");
          setProfileImageUrl(data.profileImageUrl || "https://images.pexels.com/photos/2379005/pexels-photo-2379005.jpeg?w=150&h=150&fit=crop");
          if (data.preferredInterests) {
            setSelectedInterests(data.preferredInterests.split(",").map(s => s.trim()));
          }
          if (data.interests?.length) {
            setSelectedInterests(data.interests.map(i => i.name));
          }
        }
      })
      .catch(() => {});
  }, []);

  const toggleInterest = (interest: string) => {
    setSelectedInterests(prev =>
      prev.includes(interest) ? prev.filter(i => i !== interest) : [...prev, interest]
    );
  };

  const handleSave = () => {
    if (!displayName.trim()) {
      toast.error("Display name is required");
      return;
    }

    if (authStorage.isAuthenticated()) {
      profilesApi.updateTravelerProfile({
        displayName,
        bio: bio || undefined,
        nationality: nationality || undefined,
        preferredBudgetLevel: preferredBudgetLevel || undefined,
        travelStyle: travelStyle || undefined,
        preferredInterests: selectedInterests.join(", ") || undefined,
        interestIds: [],
      }).then(() => {
        toast.success("Profile saved to server! ✨");
      }).catch(() => {
        // Still saved to localStorage - that's fine
        toast.success("Profile saved locally! ✨");
      });
    } else {
      toast.success("Profile saved locally! ✨");
    }
  };

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
      <div className="card-glass p-6 mb-6">
        <div className="flex items-center gap-4 mb-6">
          <div className="relative">
            <img
              src={profileImageUrl}
              alt="Profile"
              className="w-20 h-20 rounded-full object-cover border-2 border-accent"
              onError={(e) => {
                (e.target as HTMLImageElement).src = "https://images.pexels.com/photos/2379005/pexels-photo-2379005.jpeg?w=150&h=150&fit=crop";
              }}
            />
            <button
              onClick={() => {
                const url = prompt("Enter profile image URL:");
                if (url) setProfileImageUrl(url);
              }}
              className="absolute bottom-0 right-0 w-7 h-7 rounded-full bg-accent flex items-center justify-center hover:bg-accent/80 transition-colors"
            >
              <Camera className="w-3.5 h-3.5 text-accent-foreground" />
            </button>
          </div>
          <div>
            <h2 className="font-bold text-lg">{displayName || "Your Name"}</h2>
            <p className="text-xs text-muted-foreground">Traveler · {nationality || "Set your nationality"}</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="text-xs text-muted-foreground mb-1 block">Display Name *</label>
            <input
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Your display name"
              className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label className="text-xs text-muted-foreground mb-1 block">Nationality</label>
            <input
              value={nationality}
              onChange={(e) => setNationality(e.target.value)}
              placeholder="e.g. Egyptian, American"
              className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
          <div>
            <label className="text-xs text-muted-foreground mb-1 block">Travel Style</label>
            <div className="flex gap-2 flex-wrap">
              {travelStyleOptions.map(style => (
                <button
                  key={style}
                  onClick={() => setTravelStyle(style)}
                  className={`text-xs px-3 py-1.5 rounded-full transition-all ${
                    travelStyle === style ? "bg-accent text-accent-foreground" : "bg-secondary text-muted-foreground hover:bg-accent/20"
                  }`}
                >
                  {style}
                </button>
              ))}
            </div>
          </div>
          <div>
            <label className="text-xs text-muted-foreground mb-1 block">Budget Level</label>
            <div className="flex gap-2 flex-wrap">
              {budgetOptions.map(budget => (
                <button
                  key={budget}
                  onClick={() => setPreferredBudgetLevel(budget)}
                  className={`text-xs px-3 py-1.5 rounded-full transition-all ${
                    preferredBudgetLevel === budget ? "bg-accent text-accent-foreground" : "bg-secondary text-muted-foreground hover:bg-accent/20"
                  }`}
                >
                  {budget}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="mt-4">
          <label className="text-xs text-muted-foreground mb-1 block">Bio</label>
          <textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            placeholder="Tell us about yourself..."
            rows={3}
            className="w-full px-3 py-2 rounded-lg bg-secondary border border-border text-sm focus:outline-none focus:ring-1 focus:ring-primary resize-none"
          />
        </div>

        <div className="mt-4">
          <label className="text-xs text-muted-foreground mb-2 block">Interests</label>
          <div className="flex gap-2 flex-wrap">
            {interestOptions.map(interest => (
              <button
                key={interest}
                onClick={() => toggleInterest(interest)}
                className={`text-xs px-3 py-1.5 rounded-full transition-all flex items-center gap-1 ${
                  selectedInterests.includes(interest) ? "bg-accent text-accent-foreground" : "bg-secondary text-muted-foreground hover:bg-accent/20"
                }`}
              >
                {interest}
                {selectedInterests.includes(interest) && <Check className="w-3 h-3" />}
              </button>
            ))}
          </div>
        </div>

        <button
          onClick={handleSave}
          className="mt-6 w-full py-2.5 bg-accent text-accent-foreground rounded-xl font-medium hover:opacity-90 transition-opacity flex items-center justify-center gap-2"
        >
          <Save className="w-4 h-4" /> Save Profile
        </button>
      </div>
    </motion.div>
  );
};

export default EditProfileTab;
