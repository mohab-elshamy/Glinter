import { MapPin, Compass, Bookmark, Sparkles, Bell, MessageSquare, Settings, Calendar, Heart, Shield, DollarSign, User, Edit, Building2 } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import Navbar from "@/components/Navbar";
import { toast } from "sonner";
import { useState, useEffect } from "react";
import { useMyProfile } from "@/shared/hooks/use-my-profile";
import { createInitialsAvatar } from "@/shared/lib/avatar";
import { notificationsApi } from "@/shared/services/api-communication";
import { getSafeNotificationLink } from "@/shared/lib/notification-links";
import type { NotificationDto } from "@/shared/types/api";
import {
  BUDGET_LEVEL_STORAGE_KEY,
  budgetLevelLabel,
  budgetLevelOptions,
  parseBudgetLevel,
  readStoredBudgetLevel,
  type BudgetLevel,
} from "@/shared/lib/budget-levels";

const Dashboard = () => {
  const navigate = useNavigate();
  const profileQuery = useMyProfile();
  const profile = profileQuery.data?.profileType === "Traveler"
    ? profileQuery.data
    : undefined;
  const displayName = profile?.displayName ?? "Traveler";
  const [recentNotifications, setRecentNotifications] = useState<NotificationDto[]>([]);
  const [notificationUnread, setNotificationUnread] = useState(0);
  const [notificationsLoading, setNotificationsLoading] = useState(true);

  // Load saved preferences from localStorage
  const [selectedVibes, setSelectedVibes] = useState<string[]>(() => {
    const saved = localStorage.getItem("preferredVibes");
    return saved ? JSON.parse(saved) : ["Cultural", "Adventure"];
  });
  
  const [selectedComfort, setSelectedComfort] = useState<string>(() => {
    return localStorage.getItem("comfortLevel") || "Luxury";
  });
  
  const [selectedSafety, setSelectedSafety] = useState<string>(() => {
    return localStorage.getItem("safetyPriority") || "Very High";
  });
  
  const [selectedBudget, setSelectedBudget] = useState<BudgetLevel>(() =>
    readStoredBudgetLevel(localStorage));

  useEffect(() => {
    const profileBudget = parseBudgetLevel(profile?.preferredBudgetLevel);
    if (profileBudget) setSelectedBudget(profileBudget);
  }, [profile?.preferredBudgetLevel]);

  // Save preferences to localStorage whenever they change
  useEffect(() => {
    localStorage.setItem("preferredVibes", JSON.stringify(selectedVibes));
    localStorage.setItem("comfortLevel", selectedComfort);
    localStorage.setItem("safetyPriority", selectedSafety);
    localStorage.setItem(BUDGET_LEVEL_STORAGE_KEY, String(selectedBudget));
  }, [selectedVibes, selectedComfort, selectedSafety, selectedBudget]);

  useEffect(() => {
    notificationsApi.getNotifications(1, 3)
      .then((page) => {
        setRecentNotifications(page.items);
        setNotificationUnread(page.unreadCount);
      })
      .catch(() => {
        setRecentNotifications([]);
        setNotificationUnread(0);
      })
      .finally(() => setNotificationsLoading(false));
  }, []);

  const handleEditPreferences = () => {
    // Create a modal element
    const modal = document.createElement('div');
    modal.className = 'layer-modal-backdrop fixed inset-0 flex items-center justify-center bg-black/50 p-4 backdrop-blur-sm';
    modal.style.position = 'fixed';
    modal.style.top = '0';
    modal.style.left = '0';
    modal.style.right = '0';
    modal.style.bottom = '0';
    modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
    modal.style.backdropFilter = 'blur(4px)';
    
    modal.innerHTML = `
      <div class="bg-background border border-border rounded-2xl shadow-2xl p-6 max-w-md max-h-[90vh] overflow-y-auto" style="background: var(--background); max-width: 28rem; width: 90%;">
        <div class="flex items-center justify-between mb-6">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-accent/20 flex items-center justify-center">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="text-accent"><path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/></svg>
            </div>
            <h3 class="font-bold text-xl" style="color: var(--foreground)">Edit Preferences</h3>
          </div>
          <button class="close-modal p-1 hover:bg-secondary rounded-lg transition-colors">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>
        
        <div class="space-y-6">
          <p class="rounded-lg border border-border bg-secondary/30 p-3 text-xs text-muted-foreground">
            These comfort, safety and daily-budget preferences are saved on this device only. Account profile preferences are managed from Edit Profile.
          </p>
          <!-- Preferred Vibes -->
          <div>
            <label class="text-sm font-semibold block mb-3" style="color: var(--foreground)">Preferred Vibes</label>
            <div class="flex gap-2 flex-wrap">
              ${["Cultural", "Adventure", "Relaxation", "Nightlife"].map(vibe => `
                <button class="vibe-btn text-sm px-4 py-2 rounded-full transition-all duration-200 flex items-center gap-1 ${selectedVibes.includes(vibe) ? 'bg-accent text-accent-foreground shadow-md' : 'bg-secondary text-muted-foreground hover:bg-accent/20'}" data-vibe="${vibe}">
                  ${vibe}
                  ${selectedVibes.includes(vibe) ? '<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>' : ''}
                </button>
              `).join('')}
            </div>
          </div>

          <!-- Comfort Level -->
          <div>
            <label class="text-sm font-semibold block mb-3" style="color: var(--foreground)">Comfort Level</label>
            <div class="flex gap-2 flex-wrap">
              ${["Budget", "Mid-range", "Luxury"].map(level => `
                <button class="comfort-btn text-sm px-4 py-2 rounded-full transition-all duration-200 flex items-center gap-1 ${selectedComfort === level ? 'bg-accent text-accent-foreground shadow-md' : 'bg-secondary text-muted-foreground hover:bg-accent/20'}" data-comfort="${level}">
                  ${level}
                  ${selectedComfort === level ? '<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>' : ''}
                </button>
              `).join('')}
            </div>
          </div>

          <!-- Safety Priority -->
          <div>
            <label class="text-sm font-semibold block mb-3" style="color: var(--foreground)">Safety Priority</label>
            <div class="flex gap-2 flex-wrap">
              ${["Low", "Medium", "High", "Very High"].map(safety => `
                <button class="safety-btn text-sm px-4 py-2 rounded-full transition-all duration-200 flex items-center gap-1 ${selectedSafety === safety ? 'bg-accent text-accent-foreground shadow-md' : 'bg-secondary text-muted-foreground hover:bg-accent/20'}" data-safety="${safety}">
                  ${safety}
                  ${selectedSafety === safety ? '<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>' : ''}
                </button>
              `).join('')}
            </div>
          </div>

          <!-- Budget Range -->
          <div>
            <label class="text-sm font-semibold block mb-3" style="color: var(--foreground)">Preferred hotel budget</label>
            <div class="flex gap-2 flex-wrap">
              ${budgetLevelOptions.map(option => `
                <button class="budget-btn text-sm px-4 py-2 rounded-full transition-all duration-200 flex items-center gap-1 ${selectedBudget === option.level ? 'bg-accent text-accent-foreground shadow-md' : 'bg-secondary text-muted-foreground hover:bg-accent/20'}" data-budget="${option.level}">
                  ${option.label}
                  ${selectedBudget === option.level ? '<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>' : ''}
                </button>
              `).join('')}
            </div>
          </div>
        </div>
        
        <div class="flex gap-3 mt-8 pt-4 border-t border-border">
          <button class="save-btn flex-1 bg-accent text-accent-foreground py-2.5 rounded-xl font-medium hover:opacity-90 transition-opacity">
            Save Changes
          </button>
          <button class="cancel-btn flex-1 px-4 py-2.5 text-sm border border-border rounded-xl hover:bg-secondary transition-colors font-medium">
            Cancel
          </button>
        </div>
      </div>
    `;
    
    document.body.appendChild(modal);
    
    // Close modal function
    const closeModal = () => {
      modal.remove();
    };
    
    // Handle vibe buttons
    const vibeBtns = modal.querySelectorAll('.vibe-btn');
    let currentVibes = [...selectedVibes];
    let currentComfort = selectedComfort;
    let currentSafety = selectedSafety;
    let currentBudget = selectedBudget;
    
    vibeBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const vibe = btn.getAttribute('data-vibe');
        if (currentVibes.includes(vibe)) {
          currentVibes = currentVibes.filter(v => v !== vibe);
          btn.classList.remove('bg-accent', 'text-accent-foreground', 'shadow-md');
          btn.classList.add('bg-secondary', 'text-muted-foreground');
          btn.innerHTML = `${vibe}`;
        } else {
          currentVibes.push(vibe);
          btn.classList.add('bg-accent', 'text-accent-foreground', 'shadow-md');
          btn.classList.remove('bg-secondary', 'text-muted-foreground');
          btn.innerHTML = `${vibe}<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`;
        }
      });
    });
    
    // Handle comfort buttons
    const comfortBtns = modal.querySelectorAll('.comfort-btn');
    comfortBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const comfort = btn.getAttribute('data-comfort');
        currentComfort = comfort;
        comfortBtns.forEach(b => {
          b.classList.remove('bg-accent', 'text-accent-foreground', 'shadow-md');
          b.classList.add('bg-secondary', 'text-muted-foreground');
          b.innerHTML = b.getAttribute('data-comfort');
        });
        btn.classList.add('bg-accent', 'text-accent-foreground', 'shadow-md');
        btn.classList.remove('bg-secondary', 'text-muted-foreground');
        btn.innerHTML = `${comfort}<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`;
      });
    });
    
    // Handle safety buttons
    const safetyBtns = modal.querySelectorAll('.safety-btn');
    safetyBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const safety = btn.getAttribute('data-safety');
        currentSafety = safety;
        safetyBtns.forEach(b => {
          b.classList.remove('bg-accent', 'text-accent-foreground', 'shadow-md');
          b.classList.add('bg-secondary', 'text-muted-foreground');
          b.innerHTML = b.getAttribute('data-safety');
        });
        btn.classList.add('bg-accent', 'text-accent-foreground', 'shadow-md');
        btn.classList.remove('bg-secondary', 'text-muted-foreground');
        btn.innerHTML = `${safety}<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`;
      });
    });
    
    // Handle budget buttons
    const budgetBtns = modal.querySelectorAll('.budget-btn');
    budgetBtns.forEach(btn => {
      btn.addEventListener('click', () => {
        const budget = parseBudgetLevel(btn.getAttribute('data-budget')) ?? 3;
        currentBudget = budget;
        budgetBtns.forEach(b => {
          b.classList.remove('bg-accent', 'text-accent-foreground', 'shadow-md');
          b.classList.add('bg-secondary', 'text-muted-foreground');
          b.innerHTML = budgetLevelLabel(parseBudgetLevel(b.getAttribute('data-budget')) ?? 3);
        });
        btn.classList.add('bg-accent', 'text-accent-foreground', 'shadow-md');
        btn.classList.remove('bg-secondary', 'text-muted-foreground');
        btn.innerHTML = `${budget}<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`;
      });
    });
    
    // Save button
    const saveBtn = modal.querySelector('.save-btn');
    saveBtn.addEventListener('click', () => {
      setSelectedVibes(currentVibes);
      setSelectedComfort(currentComfort);
      setSelectedSafety(currentSafety);
      setSelectedBudget(currentBudget);
      closeModal();
      toast.success("Preferences saved successfully! ✨");
    });
    
    // Cancel button
    const cancelBtn = modal.querySelector('.cancel-btn');
    cancelBtn.addEventListener('click', closeModal);
    
    // Close button
    const closeBtn = modal.querySelector('.close-modal');
    closeBtn.addEventListener('click', closeModal);
    
    // Click outside to close
    modal.addEventListener('click', (e) => {
      if (e.target === modal) closeModal();
    });
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="flex flex-col md:flex-row items-start justify-between gap-4 mb-8">
          <div>
            <h1 className="text-2xl font-extrabold">Welcome back, {displayName} 👋</h1>
            <p className="text-sm text-muted-foreground">Your next Egyptian journey awaits.</p>
            <div className="flex flex-wrap gap-3 mt-4">
              <Link to="/where-to-go" className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-primary to-accent text-white text-sm font-medium rounded-lg hover:opacity-90 transition-opacity shadow-lg">
                <Sparkles className="w-4 h-4" /> Start New Trip
              </Link>
              <button
                onClick={() => navigate("/where-to-go")}
                className="inline-flex items-center gap-2 px-4 py-2.5 bg-secondary text-foreground text-sm font-medium rounded-lg hover:bg-accent/20 transition-opacity"
              >
                <Calendar className="w-4 h-4" /> Open Trip Planner
              </button>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button 
              onClick={() => navigate("/messages")}
              className="w-9 h-9 rounded-full bg-card border border-border flex items-center justify-center hover:bg-secondary transition-colors relative"
            >
              <Bell className="w-4 h-4 text-muted-foreground" />
              {notificationUnread > 0 && (
                <span className="absolute -top-0.5 -right-0.5 min-w-2.5 h-2.5 rounded-full bg-accent" />
              )}
            </button>
            <button 
              onClick={() => navigate("/messages")}
              className="w-9 h-9 rounded-full bg-card border border-border flex items-center justify-center hover:bg-secondary transition-colors"
            >
              <MessageSquare className="w-4 h-4 text-muted-foreground" />
            </button>
          </div>
        </div>

        {/* Tab Toggle */}
        <div className="flex rounded-lg bg-secondary p-1 mb-8 overflow-x-auto">
          <button
            onClick={() => {}}
            className="flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap bg-accent text-accent-foreground"
          >
            <User className="w-3.5 h-3.5" /> Dashboard
          </button>
          <button
            onClick={() => navigate("/profile/me")}
            className="flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap text-muted-foreground"
          >
            <Edit className="w-3.5 h-3.5" /> Edit Profile
          </button>
          <button
            onClick={() => navigate("/profile/me", { state: { tab: "bookings" } })}
            className="flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap text-muted-foreground"
          >
            <Bookmark className="w-3.5 h-3.5" /> My Bookings
          </button>
          <button
            onClick={() => navigate("/profile/me", { state: { tab: "hotels" } })}
            className="flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap text-muted-foreground"
          >
            <Building2 className="w-3.5 h-3.5" /> My Hotels
          </button>
          <button
            onClick={() => navigate("/profile/me", { state: { tab: "experiences" } })}
            className="flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap text-muted-foreground"
          >
            <Sparkles className="w-3.5 h-3.5" /> My Experiences
          </button>
        </div>

        {/* Quick Actions with different colors */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          <motion.div 
            whileHover={{ y: -5, scale: 1.02 }}
            className="card-glass p-5 cursor-pointer bg-gradient-to-br from-blue-500/10 to-blue-500/5 border-l-4 border-blue-500"
            onClick={() => navigate("/where-to-stay")}
          >
            <div className="w-10 h-10 rounded-xl bg-blue-500/20 flex items-center justify-center mb-3">
              <MapPin className="w-5 h-5 text-blue-500" />
            </div>
            <h3 className="font-bold">Where to Stay</h3>
            <p className="text-xs text-muted-foreground mb-3">Explore AI heatmaps of neighborhoods</p>
            <span className="text-xs text-blue-500 font-medium">Explore Heatmap →</span>
          </motion.div>
          
          <motion.div 
            whileHover={{ y: -5, scale: 1.02 }}
            className="card-glass p-5 cursor-pointer bg-gradient-to-br from-green-500/10 to-green-500/5 border-l-4 border-green-500"
            onClick={() => navigate("/where-to-go")}
          >
            <div className="w-10 h-10 rounded-xl bg-green-500/20 flex items-center justify-center mb-3">
              <Compass className="w-5 h-5 text-green-500" />
            </div>
            <h3 className="font-bold">Where to Go</h3>
            <p className="text-xs text-muted-foreground mb-3">Build an itinerary from real experiences</p>
            <span className="text-xs text-green-500 font-medium">Plan Itinerary →</span>
          </motion.div>
          
          <motion.div 
            whileHover={{ y: -5, scale: 1.02 }}
            className="card-glass p-5 cursor-pointer bg-gradient-to-br from-purple-500/10 to-purple-500/5 border-l-4 border-purple-500"
            onClick={() => navigate("/where-to-go")}
          >
            <div className="w-10 h-10 rounded-xl bg-purple-500/20 flex items-center justify-center mb-3">
              <Bookmark className="w-5 h-5 text-purple-500" />
            </div>
            <h3 className="font-bold">Trip Planner</h3>
            <p className="text-xs text-muted-foreground mb-3">Arrange activities into a day-by-day plan</p>
            <span className="text-xs text-purple-500 font-medium">Build a Plan →</span>
          </motion.div>
        </div>

        <div className="grid md:grid-cols-3 gap-6">
          {/* Recommendations */}
          <div className="md:col-span-2">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-bold flex items-center gap-2">
                <Sparkles className="w-4 h-4 text-primary" /> Travel Recommendations
              </h2>
              <span 
                onClick={() => navigate("/explore")}
                className="text-xs text-muted-foreground cursor-pointer hover:text-foreground transition-colors"
              >
                View All
              </span>
            </div>
            <div className="card-glass mb-6 p-8 text-center">
              <Sparkles className="mx-auto mb-3 h-8 w-8 text-muted-foreground" />
              <h3 className="text-sm font-semibold">No personalized recommendations yet</h3>
              <p className="mt-1 text-xs text-muted-foreground">
                Personalized dashboard recommendations are not configured yet.
              </p>
              <Link to="/explore" className="mt-4 inline-block text-xs font-medium text-accent">
                Explore available content →
              </Link>
            </div>

            {/* Upcoming Trip */}
            <div className="card-glass p-6 bg-gradient-to-r from-gold/5 to-accent/5">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-bold">Your Upcoming Trip</h3>
                  <p className="text-xs text-muted-foreground">Plan your adventure to Egypt</p>
                </div>
                <MapPin className="w-6 h-6 text-gold" />
              </div>
              <div className="text-center mt-6">
                <Calendar className="w-10 h-10 text-muted-foreground mx-auto mb-2" />
                <p className="text-sm text-muted-foreground mb-3">You haven't started a trip yet.</p>
                <Link to="/where-to-go" className="btn-accent text-xs py-2 px-5 rounded-lg inline-block">
                  Plan Your First Trip
                </Link>
              </div>
            </div>
          </div>

          {/* Profile Sidebar */}
          <div className="space-y-4">
            <motion.div 
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="card-glass p-5"
            >
              <h3 className="font-bold text-sm mb-4">Your Profile</h3>
              <div className="flex items-center gap-3 mb-4">
                {/* User Photo */}
                <div className="relative">
                  <img 
                    src={profile?.profileImageUrl || createInitialsAvatar(displayName)}
                    alt={displayName}
                    className="w-14 h-14 rounded-full object-cover border-2 border-accent shadow-lg"
                  />
                </div>
                <div>
                  <p className="font-bold text-base">{displayName}</p>
                  <span className="text-[10px] bg-gold/20 text-gold px-2 py-0.5 rounded-full font-medium">Traveler profile</span>
                </div>
              </div>
              <div className="space-y-2 text-xs">
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground flex items-center gap-1"><Heart className="w-3 h-3" /> Preferred Vibes</span>
                  <span>{selectedVibes.join(", ")}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground flex items-center gap-1"><Settings className="w-3 h-3" /> Comfort Level</span>
                  <span>{selectedComfort}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground flex items-center gap-1"><Shield className="w-3 h-3" /> Safety Priority</span>
                  <span>{selectedSafety}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground flex items-center gap-1"><DollarSign className="w-3 h-3" /> Hotel budget</span>
                  <span>{budgetLevelLabel(selectedBudget)}</span>
                </div>
              </div>
              <button 
                onClick={handleEditPreferences}
                className="w-full mt-4 py-2 text-sm border border-border rounded-lg hover:bg-secondary transition-colors flex items-center justify-center gap-2"
              >
                <Settings className="w-4 h-4" /> Edit Preferences
              </button>
            </motion.div>

            <div className="card-glass p-5">
              <h3 className="font-bold text-sm mb-3 flex items-center gap-1.5">
                <Bell className="w-3.5 h-3.5" /> Recent Updates
              </h3>
              <div className="space-y-3">
                {notificationsLoading && (
                  <p className="text-xs text-muted-foreground">Loading notifications…</p>
                )}
                {!notificationsLoading && recentNotifications.length === 0 && (
                  <p className="text-xs text-muted-foreground">No notifications yet.</p>
                )}
                {recentNotifications.map((notification) => (
                  <motion.button
                    key={notification.id}
                    type="button"
                    whileHover={{ x: 5 }}
                    className="block w-full rounded-lg bg-secondary/50 p-2.5 text-left"
                    onClick={() => navigate(getSafeNotificationLink(notification.linkUrl) ?? "/messages")}
                  >
                    <p className="text-xs font-medium">{notification.title}</p>
                    <p className="line-clamp-2 text-[10px] text-muted-foreground">{notification.body}</p>
                  </motion.button>
                ))}
              </div>
              <Link to="/messages" className="text-xs text-muted-foreground mt-3 inline-block cursor-pointer hover:text-foreground transition-colors">
                View All Notifications
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
