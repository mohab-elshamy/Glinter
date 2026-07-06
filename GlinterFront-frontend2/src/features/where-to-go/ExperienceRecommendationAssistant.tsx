import { useEffect, useState } from "react";
import { Bot, ChevronDown, ChevronRight, LoaderCircle, SendHorizontal, Sparkles, X } from "lucide-react";
import { experiencesApi } from "@/shared/services/api-experiences";
import type { ExperienceCategory } from "@/shared/types/api";
import type { ExperienceRecommendationResponse } from "@/shared/types/recommendations";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import { formatExperiencePrice } from "@/shared/lib/price";
import { overlayLayers } from "@/shared/lib/overlay-layers";
import { profilesApi } from "@/shared/services/api-profiles";
import { authStorage } from "@/shared/lib/auth";
import { toast } from "sonner";
import { useMyProfile } from "@/shared/hooks/use-my-profile";
import { mapInterestsToCategories } from "@/shared/lib/interest-mapping";

const categories: ExperienceCategory[] = ["Historical", "Nature", "Shopping", "Nightlife", "Dining"];

export default function ExperienceRecommendationAssistant({
  region,
  onViewDetails,
  onAddToItinerary,
}: {
  region: RegionHierarchyGids;
  onViewDetails: (id: number) => Promise<void>;
  onAddToItinerary: (id: number) => Promise<void>;
}) {
  const profileQuery = useMyProfile();
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<"natural" | "guided">("natural");
  const [text, setText] = useState("");
  const [selected, setSelected] = useState<ExperienceCategory[]>([]);
  const [limit, setLimit] = useState(5);
  const [language, setLanguage] = useState<"en" | "ar">("en");
  const [result, setResult] = useState<ExperienceRecommendationResponse>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [advanced, setAdvanced] = useState(false);
  const [visitAtLocal, setVisitAtLocal] = useState("");
  const [guestsCount, setGuestsCount] = useState(1);
  const [crowdPreference, setCrowdPreference] = useState<"Quiet" | "Balanced" | "Lively">("Balanced");
  const [bookableOnly, setBookableOnly] = useState(false);
  const [forItinerary, setForItinerary] = useState(false);
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [favoriteIds, setFavoriteIds] = useState<Set<number>>(new Set());

  useEffect(() => {
    if (!authStorage.isAuthenticated()) return;
    profilesApi.getExperienceFavoriteIds()
      .then((ids) => setFavoriteIds(new Set(ids)))
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    const profile = profileQuery.data;
    if (profile?.profileType !== "Traveler") return;
    setSelected((current) => current.length > 0
      ? current
      : mapInterestsToCategories(profile.interests.map((interest) => interest.name)));
  }, [profileQuery.data]);

  const toggleFavorite = async (experienceId: number) => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in to save experiences.");
      return;
    }
    const favorite = favoriteIds.has(experienceId);
    if (favorite) await profilesApi.removeExperienceFavorite(experienceId);
    else await profilesApi.addExperienceFavorite(experienceId);
    setFavoriteIds((current) => {
      const next = new Set(current);
      if (favorite) next.delete(experienceId); else next.add(experienceId);
      return next;
    });
  };

  const send = async () => {
    if (mode === "natural" && !text.trim()) return;
    setLoading(true);
    setError(undefined);
    try {
      const common = {
        ...region,
        limit,
        preferredLanguage: language,
        visitAtLocal: visitAtLocal || undefined,
        guestsCount,
        latitude: latitude === "" ? undefined : Number(latitude),
        longitude: longitude === "" ? undefined : Number(longitude),
        forItinerary,
      };
      setResult(mode === "natural"
        ? await experiencesApi.getNaturalLanguageRecommendations({
            ...common,
            text: text.trim(),
            crowdPreference,
            bookableOnly,
          })
        : await experiencesApi.getRecommendations({
            ...common,
            categories: selected.map((category) => ({ category })),
            crowdPreference,
            bookableOnly,
          }));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Recommendations could not be loaded.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label="Open experience recommendation assistant"
        className={`${overlayLayers.assistant} fixed bottom-5 right-5 grid h-14 w-14 place-items-center rounded-full bg-gradient-to-br from-violet-500 to-fuchsia-600 text-white shadow-[0_18px_45px_rgba(139,92,246,.45)] transition hover:scale-105 focus:outline-none focus:ring-2 focus:ring-violet-300`}
      >
        <Sparkles className="h-6 w-6" />
      </button>

      {open && (
        <section
          role="dialog"
          aria-modal="false"
          aria-labelledby="experience-assistant-title"
          className={`${overlayLayers.assistant} fixed bottom-4 right-4 flex max-h-[min(82vh,44rem)] w-[calc(100%-2rem)] max-w-md flex-col overflow-hidden rounded-3xl border border-white/10 bg-[#0d0b13] shadow-2xl sm:bottom-5 sm:right-5`}
        >
          <header className="flex items-center justify-between border-b border-white/10 bg-gradient-to-r from-violet-600/25 to-fuchsia-500/10 p-4">
            <div className="flex items-center gap-3">
              <span className="grid h-10 w-10 place-items-center rounded-2xl bg-violet-500 text-white"><Bot className="h-5 w-5" /></span>
              <div>
                <h2 id="experience-assistant-title" className="font-bold text-white">Experience match</h2>
                <p className="text-[10px] text-violet-200">AI interprets · backend ranking decides</p>
              </div>
            </div>
            <button type="button" onClick={() => setOpen(false)} aria-label="Close recommendation assistant" className="rounded-full p-2 text-slate-300 hover:bg-white/10"><X className="h-4 w-4" /></button>
          </header>

          <div className="flex-1 overflow-y-auto p-4">
            <div className="grid grid-cols-2 gap-2 rounded-xl bg-white/5 p-1">
              {(["natural", "guided"] as const).map((item) => (
                <button key={item} type="button" onClick={() => setMode(item)} className={`rounded-lg px-3 py-2 text-xs font-semibold ${mode === item ? "bg-violet-500 text-white" : "text-slate-400"}`}>
                  {item === "natural" ? "Ask naturally" : "Guided"}
                </button>
              ))}
            </div>

            {mode === "guided" && (
              <div className="mt-4 flex flex-wrap gap-2">
                {categories.map((category) => (
                  <button key={category} type="button" aria-pressed={selected.includes(category)} onClick={() => setSelected((current) => current.includes(category) ? current.filter((value) => value !== category) : [...current, category])} className={`rounded-full border px-3 py-1.5 text-xs ${selected.includes(category) ? "border-violet-400 bg-violet-500/20 text-violet-200" : "border-white/10 text-slate-400"}`}>
                    {category}
                  </button>
                ))}
              </div>
            )}

            <div className="mt-4 grid grid-cols-2 gap-3">
              <label className="text-xs text-slate-400">Number of matches
                <select value={limit} onChange={(event) => setLimit(Number(event.target.value))} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white">
                  {[3, 5, 8, 10].map((value) => <option key={value}>{value}</option>)}
                </select>
              </label>
              <label className="text-xs text-slate-400">Explanation language
                <select value={language} onChange={(event) => setLanguage(event.target.value as "en" | "ar")} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white">
                  <option value="en">English</option><option value="ar">Arabic</option>
                </select>
              </label>
            </div>

            {mode === "natural" && (
              <div className="mt-3 rounded-xl border border-white/10 bg-white/[.03] p-3 text-[10px] text-slate-400">
                Try “Quiet museums in Zamalek” or “أماكن تاريخية هادئة في الجيزة”.
              </div>
            )}

            <button
              type="button"
              aria-expanded={advanced}
              onClick={() => setAdvanced((value) => !value)}
              className="mt-4 flex w-full items-center justify-between rounded-xl border border-white/10 px-3 py-2 text-xs font-semibold text-slate-300"
            >
              Advanced preferences
              <ChevronDown className={`h-4 w-4 transition-transform ${advanced ? "rotate-180" : ""}`} />
            </button>
            {advanced && (
              <div className="mt-3 grid gap-3 rounded-xl bg-white/[.03] p-3 sm:grid-cols-2">
                <label className="text-xs text-slate-400">Visit date and time
                  <input type="datetime-local" value={visitAtLocal} onChange={(event) => setVisitAtLocal(event.target.value)} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white" />
                </label>
                <label className="text-xs text-slate-400">Guests
                  <input type="number" min="1" max="100" value={guestsCount} onChange={(event) => setGuestsCount(Number(event.target.value))} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white" />
                </label>
                <label className="text-xs text-slate-400">Crowd preference
                  <select value={crowdPreference} onChange={(event) => setCrowdPreference(event.target.value as typeof crowdPreference)} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white">
                    <option>Quiet</option><option>Balanced</option><option>Lively</option>
                  </select>
                </label>
                <div className="grid grid-cols-2 gap-2">
                  <label className="text-xs text-slate-400">Latitude<input type="number" step="any" value={latitude} onChange={(event) => setLatitude(event.target.value)} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white" /></label>
                  <label className="text-xs text-slate-400">Longitude<input type="number" step="any" value={longitude} onChange={(event) => setLongitude(event.target.value)} className="mt-1 w-full rounded-lg border border-white/10 bg-[#171520] p-2 text-white" /></label>
                </div>
                <label className="flex min-h-10 items-center gap-2 text-xs text-slate-300">
                  <input type="checkbox" checked={bookableOnly} onChange={(event) => setBookableOnly(event.target.checked)} /> Bookable only
                </label>
                <label className="flex min-h-10 items-center gap-2 text-xs text-slate-300">
                  <input type="checkbox" checked={forItinerary} onChange={(event) => setForItinerary(event.target.checked)} /> Itinerary mode
                </label>
              </div>
            )}

            {error && <p role="alert" className="mt-4 rounded-xl bg-red-500/10 p-3 text-xs text-red-200">{error}</p>}
            {loading && (
              <div role="status" className="mt-5 flex items-center gap-3 rounded-xl bg-violet-500/10 p-4 text-xs text-violet-200">
                <LoaderCircle className="h-5 w-5 animate-spin" /> Ranking eligible experiences and preparing explanations…
              </div>
            )}
            {result && !loading && (
              <div className="mt-4 space-y-3">
                <p className="text-xs text-slate-400">
                  {result.returnedCount} shown · {result.evaluatedCandidates} evaluated · {result.totalMatchingCandidates ?? result.totalCandidates} eligible
                </p>
                {result.items.map((item) => (
                  <article key={item.experienceId} className="overflow-hidden rounded-2xl border border-white/10 bg-white/[.04]">
                    {item.primaryImage && <img src={item.primaryImage} alt="" className="h-32 w-full object-cover" />}
                    <div className="p-3">
                    <div className="flex justify-between gap-3">
                      <div><p className="text-[9px] font-bold uppercase tracking-wider text-violet-300">#{item.ranking} match</p><h3 className="font-bold text-white">{item.name}</h3></div>
                      <strong className="rounded-xl bg-violet-500/15 px-2.5 py-2 text-violet-200">{item.finalScore.toFixed(0)}</strong>
                    </div>
                    <p className="mt-2 text-xs text-slate-400">{item.category} · {formatExperiencePrice(item.startingPricePerPerson, item.priceRange)} · ★ {item.rating ?? "—"} ({item.reviews ?? 0})</p>
                    <p className="mt-1 text-[10px] text-slate-500">
                      {item.regionDisplayName || "Region unavailable"} · {item.distanceKm == null ? "Distance unavailable" : `${item.distanceKm.toFixed(1)} km`} · {item.estimatedDurationMinutes} min · {item.source}
                    </p>
                    <p className={`mt-2 text-xs ${item.isBookable ? "text-emerald-300" : "text-amber-300"}`}>
                      {item.isBookable
                        ? `Bookable${item.availableCapacity != null ? ` · ${item.availableCapacity} places left` : ""}`
                        : item.availabilityDataAvailable ? "No matching slot" : "Availability not published"}
                    </p>
                    {item.nextAvailableSlot && (
                      <p className="mt-1 text-[10px] text-slate-400">Next slot {new Date(item.nextAvailableSlot.startTimeUtc).toLocaleString()}</p>
                    )}
                    {(item.openingWindow || item.recommendedVisitWindow) && (
                      <p className="mt-1 text-[10px] text-slate-400">
                        {item.openingWindow && `Open ${item.openingWindow.opensAt}–${item.openingWindow.closesAt}`}
                        {item.recommendedVisitWindow && ` · Visit ${item.recommendedVisitWindow.startLocal}–${item.recommendedVisitWindow.endLocal}`}
                      </p>
                    )}
                    {item.amenities.length > 0 && <p className="mt-2 text-[10px] text-slate-400">{item.amenities.join(" · ")}</p>}
                    <p className="mt-2 text-xs leading-5 text-slate-300">{item.explanation.shortExplanation}</p>
                    <p className="mt-1 text-[10px] text-slate-500">{item.explanation.isAiGenerated ? "AI explanation" : "Deterministic fallback explanation"}</p>
                    <details className="mt-2 rounded-lg bg-black/20 p-2 text-[10px] text-slate-400">
                      <summary className="cursor-pointer font-semibold text-slate-300">Score breakdown and reasons</summary>
                      <dl className="mt-2 grid grid-cols-2 gap-1">
                        {Object.entries(item.scores).map(([key, value]) => value == null ? null : <div key={key}><dt>{key.replace(/Score$/, "")}</dt><dd>{Number(value).toFixed(2)}</dd></div>)}
                      </dl>
                      {item.explanation.reasons.map((reason) => <p key={reason} className="mt-1">• {reason}</p>)}
                    </details>
                    <button type="button" onClick={() => void onViewDetails(item.experienceId)} className="mt-3 flex w-full items-center justify-center gap-1 rounded-xl bg-violet-500/15 px-3 py-2 text-xs font-semibold text-violet-200">
                      View full details <ChevronRight className="h-3.5 w-3.5" />
                    </button>
                    <div className="mt-2 grid grid-cols-3 gap-2">
                      <button type="button" onClick={() => void toggleFavorite(item.experienceId)} className="min-h-10 rounded-lg border border-white/10 px-2 text-[10px]">{favoriteIds.has(item.experienceId) ? "Saved" : "Favorite"}</button>
                      <button type="button" disabled={!item.isBookable} onClick={() => void onViewDetails(item.experienceId)} className="min-h-10 rounded-lg border border-white/10 px-2 text-[10px] disabled:opacity-40">Book</button>
                      <button type="button" onClick={() => void onAddToItinerary(item.experienceId)} className="min-h-10 rounded-lg border border-white/10 px-2 text-[10px]">Add to trip</button>
                    </div>
                    </div>
                  </article>
                ))}
                {result.items.length === 0 && <p className="rounded-xl bg-white/5 p-4 text-xs text-slate-400">No active approved experiences match these preferences.</p>}
              </div>
            )}
          </div>

          {mode === "natural" ? (
            <footer className="border-t border-white/10 p-3">
              <div className="flex items-end gap-2 rounded-2xl border border-white/10 bg-[#171520] p-2 focus-within:border-violet-400">
                <div className="min-w-0 flex-1">
                  <textarea value={text} maxLength={1000} onChange={(event) => setText(event.target.value)} rows={2} aria-label="Describe the experience you want" placeholder="Quiet historical places near local food…" className="w-full resize-none bg-transparent px-2 text-sm text-white outline-none placeholder:text-slate-600" />
                  <p className="px-2 text-right text-[9px] text-slate-600">{text.length}/1000</p>
                </div>
                <button type="button" onClick={() => void send()} disabled={loading || !text.trim()} aria-label="Send recommendation request" className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-violet-500 text-white disabled:opacity-40"><SendHorizontal className="h-4 w-4" /></button>
              </div>
            </footer>
          ) : (
            <footer className="border-t border-white/10 p-3">
              <button type="button" onClick={() => void send()} disabled={loading} className="w-full rounded-xl bg-violet-500 px-4 py-3 text-sm font-semibold text-white disabled:opacity-50">Find matching experiences</button>
            </footer>
          )}
        </section>
      )}
    </>
  );
}
