import { useState, useEffect, useMemo } from "react";
import { motion } from "framer-motion";
import { User, Bookmark, Building2, Sparkles, CalendarDays } from "lucide-react";
import { useNavigate, useLocation, useSearchParams } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import EditProfileTab from "./EditProfileTab";
import MyBookingsTab from "./MyBookingsTab";
import MyHotelsTab from "./MyHotelsTab";
import MyExperiencesTab from "./MyExperiencesTab";
import { authStorage } from "@/shared/lib/auth";
import MyBuddyScheduleTab from "./MyBuddyScheduleTab";

const allTabs = [
  { key: "edit", label: "Edit Profile", icon: User },
  { key: "bookings", label: "My Bookings", icon: Bookmark },
  { key: "hotels", label: "My Hotels", icon: Building2 },
  { key: "experiences", label: "My Experiences", icon: Sparkles },
  { key: "buddy-schedule", label: "Schedule & Requests", icon: CalendarDays },
] as const;

type TabKey = (typeof allTabs)[number]["key"];

const EditProfilePage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const roles = authStorage.getUser()?.roles ?? [];
  const roleKind = roles.includes("HotelOwner")
    ? "hotel-owner"
    : roles.includes("ExperienceProvider")
      ? "experience-provider"
      : roles.includes("LocalBuddy")
        ? "local-buddy"
        : "traveler";
  const tabs = useMemo(() => {
    const allowedTabKeys: readonly TabKey[] = roleKind === "hotel-owner"
      ? ["edit", "hotels"]
      : roleKind === "experience-provider"
        ? ["edit", "experiences"]
      : roleKind === "local-buddy"
          ? ["edit", "buddy-schedule"]
          : ["edit", "bookings"];
    return allTabs.filter((tab) => allowedTabKeys.includes(tab.key));
  }, [roleKind]);
  const defaultTab = tabs[0]?.key ?? "edit";
  const [activeTab, setActiveTab] = useState<TabKey>(defaultTab);

  useEffect(() => {
    const tabFromState = (location.state as { tab?: TabKey } | null)?.tab;
    const tabFromQuery = searchParams.get("tab") as TabKey | null;
    const requestedTab = tabFromState ?? tabFromQuery;
    if (requestedTab && tabs.some((tab) => tab.key === requestedTab)) {
      setActiveTab(requestedTab);
    } else if (!tabs.some((tab) => tab.key === activeTab)) {
      setActiveTab(defaultTab);
    }
  }, [activeTab, defaultTab, location.state, searchParams, tabs]);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <div className="container mx-auto px-4 py-8 max-w-4xl">
        <div className="flex items-center gap-3 mb-6">
          <User className="w-6 h-6 text-accent" />
          <h1 className="text-2xl font-bold">My Profile</h1>
        </div>

        <div className="flex rounded-lg bg-secondary p-1 mb-8 overflow-x-auto">
          {roles.includes("Traveler") && (
            <button
              onClick={() => navigate("/dashboard")}
              className="flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap text-muted-foreground"
            >
              <User className="w-3.5 h-3.5" /> Dashboard
            </button>
          )}
          {tabs.map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              onClick={() => setActiveTab(key)}
              className={`flex-1 py-2 text-xs font-medium rounded-md transition-all flex items-center justify-center gap-1.5 whitespace-nowrap ${
                activeTab === key ? "bg-accent text-accent-foreground" : "text-muted-foreground"
              }`}
            >
              <Icon className="w-3.5 h-3.5" />
              {label}
            </button>
          ))}
        </div>

        <motion.div
          key={activeTab}
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.16 }}
        >
          {activeTab === "edit" && <EditProfileTab />}
          {activeTab === "bookings" && <MyBookingsTab />}
          {activeTab === "hotels" && <MyHotelsTab />}
          {activeTab === "experiences" && <MyExperiencesTab />}
          {activeTab === "buddy-schedule" && <MyBuddyScheduleTab />}
        </motion.div>
      </div>
      <Footer />
    </div>
  );
};

export default EditProfilePage;
