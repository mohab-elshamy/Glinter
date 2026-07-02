import { useEffect, useMemo, useState } from "react";
import { Bell, Bookmark, Calendar, Compass, DollarSign, Heart, MapPin, MessageSquare, Settings, Shield, Sparkles } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import Navbar from "@/components/Navbar";
import { Dialog } from "@/components/ui/dialog";
import { useMyProfile } from "@/shared/hooks/use-my-profile";
import { notificationsApi } from "@/shared/services/api-communication";
import { staysApi } from "@/shared/services/api-stays";
import { experiencesApi } from "@/shared/services/api-experiences";
import { itinerariesApi } from "@/shared/services/api-itineraries";
import { profilesApi } from "@/shared/services/api-profiles";
import { createInitialsAvatar } from "@/shared/lib/avatar";
import { getSafeNotificationLink } from "@/shared/lib/notification-links";
import { mapInterestsToCategories } from "@/shared/lib/interest-mapping";
import {
  budgetLevelLabel,
  budgetLevelOptions,
  budgetLevelToProfileString,
  parseBudgetLevel,
  readStoredBudgetLevel,
  type BudgetLevel,
} from "@/shared/lib/budget-levels";
import { formatExperiencePrice, formatUsdPrice } from "@/shared/lib/price";
import type { ExperienceCategory, NotificationDto } from "@/shared/types/api";
import { toast } from "sonner";

const Dashboard = () => {
  const navigate = useNavigate();
  const profileQuery = useMyProfile();
  const profile = profileQuery.data?.profileType === "Traveler" ? profileQuery.data : undefined;
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unread, setUnread] = useState(0);
  const [preferencesOpen, setPreferencesOpen] = useState(false);
  const [budget, setBudget] = useState<BudgetLevel>(() => readStoredBudgetLevel(localStorage));
  const [vibes, setVibes] = useState<string[]>([]);
  const [comfort, setComfort] = useState("Mid-range");
  const [safety, setSafety] = useState("High");
  const [savingPreferences, setSavingPreferences] = useState(false);

  const categories: ExperienceCategory[] = useMemo(
    () => mapInterestsToCategories((profile?.interests ?? []).map((interest) => interest.name)),
    [profile?.interests],
  );
  const hasPreferences = categories.length > 0 || Boolean(profile?.preferredBudgetLevel);

  useEffect(() => {
    if (!profile) return;
    setBudget(parseBudgetLevel(profile.preferredBudgetLevel) ?? readStoredBudgetLevel(localStorage));
    setVibes(profile.preferredVibes?.split(",").map((value) => value.trim()).filter(Boolean) ?? []);
    setComfort(profile.comfortLevel ?? "Mid-range");
    setSafety(profile.safetyPriority ?? "High");
  }, [profile]);

  useEffect(() => {
    if (!profile || localStorage.getItem("dashboardPreferencesMigrated") === "true") return;
    const storedVibes = localStorage.getItem("preferredVibes");
    const storedComfort = localStorage.getItem("comfortLevel");
    const storedSafety = localStorage.getItem("safetyPriority");
    if (!storedVibes && !storedComfort && !storedSafety) {
      localStorage.setItem("dashboardPreferencesMigrated", "true");
      return;
    }
    let migratedVibes: string[] = [];
    try {
      const parsed: unknown = storedVibes ? JSON.parse(storedVibes) : [];
      if (Array.isArray(parsed)) migratedVibes = parsed.filter((value): value is string => typeof value === "string");
    } catch {
      migratedVibes = [];
    }
    void profilesApi.updateTravelerProfile({
      displayName: profile.displayName,
      bio: profile.bio,
      nationality: profile.nationality,
      preferredBudgetLevel: profile.preferredBudgetLevel,
      travelStyle: profile.travelStyle,
      preferredInterests: profile.preferredInterests,
      preferredVibes: profile.preferredVibes || migratedVibes.join(", ") || undefined,
      comfortLevel: profile.comfortLevel || storedComfort || undefined,
      safetyPriority: profile.safetyPriority || storedSafety || undefined,
      interestIds: profile.interests.map((interest) => interest.id),
    }).then(() => {
      localStorage.setItem("dashboardPreferencesMigrated", "true");
      void profileQuery.refetch();
    }).catch(() => undefined);
  }, [profile, profileQuery]);

  useEffect(() => {
    notificationsApi.getNotifications(1, 3)
      .then((page) => { setNotifications(page.items); setUnread(page.unreadCount); })
      .catch(() => { setNotifications([]); setUnread(0); });
  }, []);

  const stays = useQuery({
    queryKey: ["dashboard-stays", profile?.profileId, budget, categories.join(",")],
    queryFn: () => staysApi.getRecommendations({
      budgetLevel: budget,
      experienceCategories: categories.map((category) => ({ category })),
      requestedAmenities: [],
      limit: 3,
      preferredLanguage: "en",
    }),
    enabled: hasPreferences,
    staleTime: 10 * 60_000,
    retry: false,
  });
  const experiences = useQuery({
    queryKey: ["dashboard-experiences", profile?.profileId, categories.join(",")],
    queryFn: () => experiencesApi.getRecommendations({
      categories: categories.map((category) => ({ category })),
      limit: 3,
      preferredLanguage: "en",
    }),
    enabled: hasPreferences,
    staleTime: 10 * 60_000,
    retry: false,
  });
  const trips = useQuery({
    queryKey: ["dashboard-trips"],
    queryFn: itinerariesApi.list,
    staleTime: 60_000,
    retry: false,
  });
  const stayBookings = useQuery({
    queryKey: ["dashboard-stay-bookings"],
    queryFn: staysApi.getMyBookings,
    retry: false,
  });
  const experienceBookings = useQuery({
    queryKey: ["dashboard-experience-bookings"],
    queryFn: experiencesApi.getMyBookings,
    retry: false,
  });
  const buddyBookings = useQuery({
    queryKey: ["dashboard-buddy-bookings"],
    queryFn: profilesApi.getMyBuddyBookings,
    retry: false,
  });
  const today = new Date().toISOString().slice(0, 10);
  const upcoming = [...(trips.data ?? [])]
    .filter((trip) => trip.startDate >= today)
    .sort((left, right) => left.startDate.localeCompare(right.startDate))[0];
  const upcomingBooking = [
    ...(stayBookings.data ?? []).filter((booking) =>
      booking.checkInDate >= today && booking.status !== "Cancelled" && booking.status !== "Completed")
      .map((booking) => ({
        id: booking.id,
        title: booking.stayName,
        startsAt: booking.checkInDate,
        endsAt: booking.checkOutDate,
        type: "Stay booking",
      })),
    ...(experienceBookings.data ?? []).filter((booking) =>
      booking.startTimeUtc.slice(0, 10) >= today && booking.status !== "Cancelled" && booking.status !== "Completed")
      .map((booking) => ({
        id: booking.id,
        title: booking.experienceName,
        startsAt: booking.startTimeUtc,
        endsAt: booking.endTimeUtc,
        type: "Experience booking",
      })),
    ...(buddyBookings.data ?? []).filter((booking) =>
      booking.startTimeUtc.slice(0, 10) >= today && booking.status !== "Cancelled" && booking.status !== "Completed" && booking.status !== "Rejected")
      .map((booking) => ({
        id: booking.id,
        title: booking.buddyName,
        startsAt: booking.startTimeUtc,
        endsAt: booking.endTimeUtc,
        type: "Buddy booking",
      })),
  ].sort((left, right) => left.startsAt.localeCompare(right.startsAt))[0];
  const upcomingLoading = trips.isLoading || stayBookings.isLoading ||
    experienceBookings.isLoading || buddyBookings.isLoading;

  const savePreferences = async () => {
    if (!profile) return;
    setSavingPreferences(true);
    try {
      await profilesApi.updateTravelerProfile({
        displayName: profile.displayName,
        bio: profile.bio,
        nationality: profile.nationality,
        preferredBudgetLevel: budgetLevelToProfileString(budget),
        travelStyle: profile.travelStyle,
        preferredInterests: profile.preferredInterests,
        preferredVibes: vibes.join(", ") || undefined,
        comfortLevel: comfort,
        safetyPriority: safety,
        interestIds: profile.interests.map((interest) => interest.id),
      });
      localStorage.setItem("dashboardPreferencesMigrated", "true");
      localStorage.removeItem("preferredVibes");
      localStorage.removeItem("comfortLevel");
      localStorage.removeItem("safetyPriority");
      setPreferencesOpen(false);
      await profileQuery.refetch();
      toast.success("Preferences saved to your traveler profile.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not save preferences.");
    } finally {
      setSavingPreferences(false);
    }
  };

  const displayName = profile?.displayName ?? "Traveler";
  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-7xl px-4 py-8">
        <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
          <div>
            <h1 className="text-2xl font-extrabold">Welcome back, {displayName}</h1>
            <p className="text-sm text-muted-foreground">Your trip data and deterministic recommendations in one place.</p>
            <div className="mt-4 flex flex-wrap gap-2">
              <Link to="/where-to-go" className="btn-accent rounded-lg px-4 py-2 text-sm"><Sparkles className="mr-2 inline h-4 w-4" />Plan a trip</Link>
              <Link to="/saved" className="rounded-lg border border-border px-4 py-2 text-sm"><Bookmark className="mr-2 inline h-4 w-4" />Saved travel</Link>
            </div>
          </div>
          <div className="flex gap-2">
            <button onClick={() => navigate("/messages")} aria-label="Notifications" className="relative rounded-full border border-border p-2.5"><Bell className="h-4 w-4" />{unread > 0 && <span className="absolute right-0 top-0 h-2.5 w-2.5 rounded-full bg-accent" />}</button>
            <button onClick={() => navigate("/messages")} aria-label="Messages" className="rounded-full border border-border p-2.5"><MessageSquare className="h-4 w-4" /></button>
          </div>
        </header>

        <section className="mt-8 grid gap-4 sm:grid-cols-3">
          <QuickLink to="/where-to-stay" icon={MapPin} title="Where to stay" text="Browse stays and personalized matches" />
          <QuickLink to="/where-to-go" icon={Compass} title="Where to go" text="Build one complete multi-day plan" />
          <QuickLink to="/saved" icon={Heart} title="Saved travel" text="Edit trips and open hotel details" />
        </section>

        <div className="mt-8 grid gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)]">
          <div className="space-y-6">
            <section>
              <div className="mb-3 flex items-center justify-between"><h2 className="font-bold"><Sparkles className="mr-2 inline h-4 w-4 text-primary" />Recommendations</h2></div>
              {!hasPreferences ? (
                <div className="rounded-2xl border border-dashed border-border p-8 text-center"><p className="text-sm text-muted-foreground">Add profile interests or a budget to receive matches.</p><Link to="/profile/me" className="mt-3 inline-block text-sm text-accent">Complete profile</Link></div>
              ) : (
                <div className="grid gap-4 md:grid-cols-2">
                  <RecommendationList title="Stays" loading={stays.isLoading} error={stays.isError} items={(stays.data?.items ?? []).map((item) => ({
                    id: item.hotelId,
                    name: item.name,
                    score: item.finalScore,
                    meta: `${item.price == null ? "Price unavailable" : formatUsdPrice(item.price)} · ★ ${item.rating ?? "—"}`,
                    explanation: item.explanation.shortExplanation,
                    link: `/where-to-stay?stayId=${item.hotelId}`,
                  }))} />
                  <RecommendationList title="Experiences" loading={experiences.isLoading} error={experiences.isError} items={(experiences.data?.items ?? []).map((item) => ({
                    id: item.experienceId,
                    name: item.name,
                    score: item.finalScore,
                    meta: `${formatExperiencePrice(item.startingPricePerPerson, item.priceRange)} · ★ ${item.rating ?? "—"}`,
                    explanation: item.explanation.shortExplanation,
                    link: `/where-to-go?experienceId=${item.experienceId}`,
                  }))} />
                </div>
              )}
            </section>

            <section className="rounded-2xl border border-border bg-card p-6">
              <h2 className="font-bold">Upcoming trip</h2>
              {upcomingLoading ? <p role="status" className="mt-4 text-sm text-muted-foreground">Loading upcoming travel…</p>
                : upcoming ? <div className="mt-4 rounded-xl bg-secondary/40 p-4"><h3 className="font-semibold">{upcoming.title}</h3><p className="text-xs text-muted-foreground">{upcoming.destination || "Destination not named"}</p><p className="mt-2 text-xs">{upcoming.startDate} – {upcoming.endDate} · {upcoming.items.length} stops</p><div className="mt-4 flex gap-2"><Link to={`/itineraries/${upcoming.id}`} className="btn-accent rounded-lg px-4 py-2 text-xs">Open trip</Link><Link to="/saved" className="rounded-lg border border-border px-4 py-2 text-xs">View all</Link></div></div>
                  : upcomingBooking ? <div className="mt-4 rounded-xl bg-secondary/40 p-4"><h3 className="font-semibold">{upcomingBooking.title}</h3><p className="text-xs text-muted-foreground">{upcomingBooking.type}</p><p className="mt-2 text-xs">{new Date(upcomingBooking.startsAt).toLocaleString()} – {new Date(upcomingBooking.endsAt).toLocaleString()}</p><div className="mt-4 flex gap-2"><Link to="/profile/me?tab=bookings" className="btn-accent rounded-lg px-4 py-2 text-xs">Manage booking</Link><Link to="/saved" className="rounded-lg border border-border px-4 py-2 text-xs">View trips</Link></div></div>
                  : <div className="mt-5 text-center"><Calendar className="mx-auto h-9 w-9 text-muted-foreground" /><p className="mt-2 text-sm text-muted-foreground">No upcoming saved itinerary.</p><Link to="/where-to-go" className="mt-3 inline-block text-sm text-accent">Plan a trip</Link></div>}
            </section>
          </div>

          <aside className="space-y-5">
            <section className="rounded-2xl border border-border bg-card p-5">
              <div className="flex items-center gap-3"><img src={profile?.profileImageUrl || createInitialsAvatar(displayName)} alt={displayName} className="h-14 w-14 rounded-full object-cover" /><div><h2 className="font-bold">{displayName}</h2><p className="text-xs text-muted-foreground">Traveler profile</p></div></div>
              <dl className="mt-4 space-y-2 text-xs"><Preference icon={Heart} label="Vibes" value={vibes.join(", ") || "Not set"} /><Preference icon={Settings} label="Comfort" value={comfort} /><Preference icon={Shield} label="Safety" value={safety} /><Preference icon={DollarSign} label="Budget" value={budgetLevelLabel(budget)} /></dl>
              <button onClick={() => setPreferencesOpen(true)} className="mt-4 w-full rounded-lg border border-border py-2 text-sm"><Settings className="mr-2 inline h-4 w-4" />Edit preferences</button>
              <p className="mt-3 text-[10px] text-muted-foreground">Budget and mapped interests affect current recommendations. Comfort and safety are stored for profile use but do not alter ranking yet.</p>
            </section>

            <section className="rounded-2xl border border-border bg-card p-5"><h2 className="font-bold">Recent updates</h2><div className="mt-3 space-y-2">{notifications.map((notification) => <button key={notification.id} onClick={() => navigate(getSafeNotificationLink(notification.linkUrl) ?? "/messages")} className="block w-full rounded-lg bg-secondary/40 p-3 text-left"><strong className="text-xs">{notification.title}</strong><p className="line-clamp-2 text-[10px] text-muted-foreground">{notification.body}</p></button>)}{notifications.length === 0 && <p className="text-xs text-muted-foreground">No notifications yet.</p>}</div></section>
          </aside>
        </div>
      </main>

      <Dialog open={preferencesOpen} onOpenChange={setPreferencesOpen} title="Dashboard preferences" description="These values are saved to your traveler profile.">
        <div className="space-y-5">
          <fieldset><legend className="text-sm font-semibold">Preferred vibes</legend><div className="mt-2 flex flex-wrap gap-2">{["Cultural", "Adventure", "Relaxation", "Nightlife"].map((vibe) => <button key={vibe} type="button" aria-pressed={vibes.includes(vibe)} onClick={() => setVibes((current) => current.includes(vibe) ? current.filter((value) => value !== vibe) : [...current, vibe])} className={`rounded-full border px-3 py-2 text-xs ${vibes.includes(vibe) ? "border-primary bg-primary/15 text-primary" : "border-border"}`}>{vibe}</button>)}</div></fieldset>
          <label className="block text-sm font-semibold">Comfort level<select value={comfort} onChange={(event) => setComfort(event.target.value)} className="mt-2 w-full rounded-lg border border-border bg-background p-2"><option>Budget</option><option>Mid-range</option><option>Luxury</option></select></label>
          <label className="block text-sm font-semibold">Safety priority<select value={safety} onChange={(event) => setSafety(event.target.value)} className="mt-2 w-full rounded-lg border border-border bg-background p-2"><option>Low</option><option>Medium</option><option>High</option><option>Very High</option></select></label>
          <label className="block text-sm font-semibold">Hotel budget<select value={budget} onChange={(event) => setBudget(Number(event.target.value) as BudgetLevel)} className="mt-2 w-full rounded-lg border border-border bg-background p-2">{budgetLevelOptions.map((option) => <option key={option.level} value={option.level}>{option.label}</option>)}</select></label>
          <div className="flex justify-end gap-2"><button type="button" onClick={() => setPreferencesOpen(false)} className="rounded-lg border border-border px-4 py-2 text-sm">Cancel</button><button type="button" disabled={savingPreferences} onClick={() => void savePreferences()} className="btn-accent rounded-lg px-4 py-2 text-sm disabled:opacity-60">{savingPreferences ? "Saving…" : "Save preferences"}</button></div>
        </div>
      </Dialog>
    </div>
  );
};

const QuickLink = ({ to, icon: Icon, title, text }: { to: string; icon: typeof MapPin; title: string; text: string }) => <Link to={to} className="rounded-2xl border border-border bg-card p-5 transition hover:-translate-y-1"><Icon className="h-5 w-5 text-primary" /><h2 className="mt-3 font-bold">{title}</h2><p className="text-xs text-muted-foreground">{text}</p></Link>;
const Preference = ({ icon: Icon, label, value }: { icon: typeof Heart; label: string; value: string }) => <div className="flex justify-between gap-3"><dt className="flex items-center gap-1 text-muted-foreground"><Icon className="h-3 w-3" />{label}</dt><dd className="text-right">{value}</dd></div>;

interface RecommendationItem { id: number; name: string; score: number; meta: string; explanation: string; link: string }
const RecommendationList = ({ title, loading, error, items }: { title: string; loading: boolean; error: boolean; items: RecommendationItem[] }) => <section className="rounded-2xl border border-border bg-card p-4"><h3 className="font-semibold">{title}</h3>{loading && <p role="status" className="mt-3 text-xs text-muted-foreground">Finding matches…</p>}{error && <p className="mt-3 text-xs text-destructive">Recommendations are temporarily unavailable.</p>}<div className="mt-3 space-y-3">{items.map((item) => <article key={item.id} className="rounded-xl border border-border bg-background/50 p-3"><div className="flex justify-between gap-2"><h4 className="text-sm font-semibold">{item.name}</h4><span className="rounded-full bg-primary/10 px-2 py-1 text-[10px] font-bold text-primary">{item.score.toFixed(0)}</span></div><p className="mt-1 text-[10px] text-muted-foreground">{item.meta}</p><p className="mt-2 line-clamp-2 text-xs text-muted-foreground">{item.explanation}</p><Link to={item.link} className="mt-2 inline-block text-xs font-semibold text-accent">View details →</Link></article>)}{!loading && !error && items.length === 0 && <p className="text-xs text-muted-foreground">No eligible matches found.</p>}</div></section>;

export default Dashboard;
