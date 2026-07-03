import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import {
  Bot,
  ChevronRight,
  MapPin,
  RotateCcw,
  SendHorizontal,
  SlidersHorizontal,
  Sparkles,
  Star,
  X,
} from "lucide-react";
import { staysApi } from "@/shared/services/api-stays";
import type { HotelRecommendationResponse } from "@/shared/types/api";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import {
  budgetLevelOptions,
  parseBudgetLevel,
  readStoredBudgetLevel,
  type BudgetLevel,
} from "@/shared/lib/budget-levels";
import { useMyProfile } from "@/shared/hooks/use-my-profile";
import { formatUsdPrice } from "@/shared/lib/price";
import { overlayLayers } from "@/shared/lib/overlay-layers";
import {
  buildStructuredRecommendationRequest,
  NATURAL_LANGUAGE_MAX_CHARACTERS,
} from "../recommendation-form";
import { mapInterestsToCategories } from "@/shared/lib/interest-mapping";

const categories = ["Historical", "Nature", "Shopping", "Nightlife", "Dining"] as const;
const amenities = ["WiFi", "Gym", "Pool", "Spa", "Restaurant", "Bar", "Parking"] as const;
const examples = [
  "A calm mid-range hotel near museums",
  "Luxury stay with a pool and good restaurants",
  "Budget hotel near nature and shopping",
];

interface HotelRecommendationPanelProps {
  region: RegionHierarchyGids;
  onViewDetails: (hotelId: number) => Promise<void>;
}

type Mode = "natural" | "guided";
type ResultState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; message: string }
  | { status: "ready"; response: HotelRecommendationResponse };

const HotelRecommendationPanel = ({
  region,
  onViewDetails,
}: HotelRecommendationPanelProps) => {
  const profileQuery = useMyProfile();
  const manuallyChangedBudget = useRef(false);
  const retryRef = useRef<(() => Promise<void>) | null>(null);
  const conversationEndRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<Mode>("natural");
  const [budgetLevel, setBudgetLevel] = useState<BudgetLevel | undefined>(() =>
    readStoredBudgetLevel(localStorage));
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  const [categoryWeights, setCategoryWeights] = useState<Record<string, number | undefined>>({});
  const [selectedAmenities, setSelectedAmenities] = useState<string[]>([]);
  const [limit, setLimit] = useState(5);
  const [language, setLanguage] = useState<"en" | "ar">("en");
  const [naturalText, setNaturalText] = useState("");
  const [lastMessage, setLastMessage] = useState("");
  const [state, setState] = useState<ResultState>({ status: "idle" });
  const [detailsLoadingId, setDetailsLoadingId] = useState<number>();

  useEffect(() => {
    if (manuallyChangedBudget.current || profileQuery.data?.profileType !== "Traveler") return;
    const profileBudget = parseBudgetLevel(profileQuery.data.preferredBudgetLevel);
    if (profileBudget) setBudgetLevel(profileBudget);
    setSelectedCategories((current) => current.length > 0
      ? current
      : mapInterestsToCategories(profileQuery.data.interests.map((interest) => interest.name)));
  }, [profileQuery.data]);

  useEffect(() => {
    if (open && state.status !== "idle") {
      const conversationEnd = conversationEndRef.current;
      if (typeof conversationEnd?.scrollIntoView === "function") {
        conversationEnd.scrollIntoView({ behavior: "smooth", block: "end" });
      }
    }
  }, [open, state]);

  const runGuided = async () => {
    const request = buildStructuredRecommendationRequest({
      budgetLevel,
      selectedCategories,
      categoryWeights,
      selectedAmenities,
      limit,
      language,
      region,
    });
    const summary = [
      budgetLevel ? `budget level ${budgetLevel}` : "any budget",
      selectedCategories.length > 0 ? selectedCategories.join(", ") : "any nearby experiences",
      selectedAmenities.length > 0 ? selectedAmenities.join(", ") : "no required amenities",
    ].join(" · ");

    setLastMessage(summary);
    setState({ status: "loading" });
    retryRef.current = runGuided;
    try {
      setState({ status: "ready", response: await staysApi.getRecommendations(request) });
    } catch (error) {
      setState({
        status: "error",
        message: error instanceof Error ? error.message : "Recommendations could not be loaded.",
      });
    }
  };

  const runNatural = async (text: string) => {
    if (!text || text.length > NATURAL_LANGUAGE_MAX_CHARACTERS) return;
    const request = {
      text,
      limit,
      adm0Gid: region.adm0Gid,
      adm1Gid: region.adm1Gid,
      adm2Gid: region.adm2Gid,
      adm3Gid: region.adm3Gid,
      preferredLanguage: language,
    };

    setLastMessage(text);
    setNaturalText("");
    setState({ status: "loading" });
    retryRef.current = () => runNatural(text);
    try {
      setState({
        status: "ready",
        response: await staysApi.getNaturalLanguageRecommendations(request),
      });
    } catch (error) {
      setState({
        status: "error",
        message: error instanceof Error ? error.message : "Recommendations could not be loaded.",
      });
    }
  };

  const openDetails = async (hotelId: number) => {
    setDetailsLoadingId(hotelId);
    try {
      await onViewDetails(hotelId);
    } finally {
      setDetailsLoadingId(undefined);
    }
  };

  const regionValues = [region.adm0Gid, region.adm1Gid, region.adm2Gid, region.adm3Gid]
    .filter((value) => value != null);

  const chatbot = (
    <>
      {open && (
        <section
          role="dialog"
          aria-label="AI stay recommendation assistant"
          className={`${overlayLayers.assistant} fixed bottom-[5.75rem] left-3 right-3 flex h-[min(700px,calc(100dvh-7rem))] flex-col overflow-hidden rounded-[1.75rem] border border-violet-400/20 bg-[#0d0c14]/95 shadow-[0_24px_90px_rgba(0,0,0,.65),0_0_50px_rgba(124,58,237,.14)] backdrop-blur-2xl sm:left-auto sm:right-5 sm:w-[430px]`}
        >
          <header className="relative overflow-hidden border-b border-white/10 px-4 py-4">
            <div className="absolute -right-16 -top-20 h-40 w-40 rounded-full bg-violet-600/25 blur-3xl" />
            <div className="relative flex items-center gap-3">
              <div className="relative grid h-11 w-11 shrink-0 place-items-center rounded-2xl bg-gradient-to-br from-violet-500 to-fuchsia-600 text-white shadow-lg shadow-violet-900/40">
                <Bot className="h-5 w-5" />
                <span className="absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-[#0d0c14] bg-emerald-400" />
              </div>
              <div className="min-w-0 flex-1">
                <h2 className="flex items-center gap-2 font-bold text-white">
                  Glinter AI Stay Assistant
                  <span className="rounded-full bg-violet-400/10 px-2 py-0.5 text-[9px] font-semibold uppercase tracking-wider text-violet-300">AI</span>
                </h2>
                <p className="mt-0.5 text-[11px] text-slate-400">Tell me what your ideal stay looks like</p>
              </div>
              <button
                type="button"
                onClick={() => setOpen(false)}
                aria-label="Close AI stay assistant"
                className="grid h-9 w-9 place-items-center rounded-xl border border-white/10 text-slate-400 transition hover:bg-white/10 hover:text-white"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </header>

          <div className="border-b border-white/8 px-4 py-3">
            <div className="grid grid-cols-2 rounded-xl bg-white/[0.04] p-1" role="tablist" aria-label="Recommendation mode">
              <button
                type="button"
                role="tab"
                aria-selected={mode === "natural"}
                onClick={() => setMode("natural")}
                className={`flex items-center justify-center gap-2 rounded-lg px-3 py-2 text-xs font-semibold transition ${mode === "natural" ? "bg-violet-500 text-white shadow-lg shadow-violet-950/30" : "text-slate-400 hover:text-white"}`}
              >
                <Sparkles className="h-3.5 w-3.5" /> Ask AI
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={mode === "guided"}
                onClick={() => setMode("guided")}
                className={`flex items-center justify-center gap-2 rounded-lg px-3 py-2 text-xs font-semibold transition ${mode === "guided" ? "bg-violet-500 text-white shadow-lg shadow-violet-950/30" : "text-slate-400 hover:text-white"}`}
              >
                <SlidersHorizontal className="h-3.5 w-3.5" /> Preferences
              </button>
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4 [scrollbar-color:#3b3650_transparent]">
            <div className="flex items-start gap-2.5">
              <div className="mt-0.5 grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-violet-500/15 text-violet-300">
                <Bot className="h-3.5 w-3.5" />
              </div>
              <div className="max-w-[88%] rounded-2xl rounded-tl-md border border-white/8 bg-white/[0.045] px-3.5 py-3 text-xs leading-5 text-slate-300">
                <p>Hi! Describe the hotel you want and I’ll rank the best active stays for you.</p>
                <p className="mt-2 text-[10px] text-slate-500">Ranking uses hotel data, your budget, amenities, quality, and nearby experiences.</p>
              </div>
            </div>

            <div className="ml-9 mt-3 flex items-center gap-1.5 text-[10px] text-slate-500">
              <MapPin className="h-3 w-3 text-violet-400" />
              {regionValues.length > 0
                ? `Using your active region filter (${regionValues.join(" › ")})`
                : "Searching all regions — no location is being assumed"}
            </div>

            <div className="mt-4 grid grid-cols-2 gap-2">
              <label className="text-[10px] font-medium text-slate-400">
                Number of recommendations
                <select
                  value={limit}
                  onChange={(event) => setLimit(Number(event.target.value))}
                  className="mt-1.5 h-10 w-full rounded-xl border border-white/10 bg-[#171520] px-3 text-xs text-white outline-none [color-scheme:dark] focus:border-violet-400/60"
                >
                  {[3, 5, 10, 15, 20, 25].map((value) => (
                    <option key={value} value={value}>{value} hotel matches</option>
                  ))}
                </select>
              </label>
              <label className="text-[10px] font-medium text-slate-400">
                Reply language
                <select
                  value={language}
                  onChange={(event) => setLanguage(event.target.value as "en" | "ar")}
                  className="mt-1.5 h-10 w-full rounded-xl border border-white/10 bg-[#171520] px-3 text-xs text-white outline-none [color-scheme:dark] focus:border-violet-400/60"
                >
                  <option value="en">English</option>
                  <option value="ar">Arabic</option>
                </select>
              </label>
            </div>
            <p className="mt-1.5 text-[10px] leading-4 text-slate-500">
              This controls how many ranked hotel suggestions I return — it does not limit normal search results.
            </p>

            {mode === "natural" ? (
              state.status === "idle" && (
                <div className="mt-5">
                  <p className="mb-2 text-[10px] font-semibold uppercase tracking-[0.14em] text-slate-500">Try an example</p>
                  <div className="space-y-2">
                    {examples.map((example) => (
                      <button
                        key={example}
                        type="button"
                        onClick={() => setNaturalText(example)}
                        className="group flex w-full items-center justify-between gap-2 rounded-xl border border-white/8 bg-white/[0.025] px-3 py-2.5 text-left text-xs text-slate-400 transition hover:border-violet-400/30 hover:bg-violet-500/[0.06] hover:text-slate-200"
                      >
                        <span>{example}</span>
                        <ChevronRight className="h-3.5 w-3.5 shrink-0 text-slate-600 transition group-hover:translate-x-0.5 group-hover:text-violet-400" />
                      </button>
                    ))}
                  </div>
                </div>
              )
            ) : (
              <div className="mt-5 space-y-5">
                <label className="block text-[10px] font-medium text-slate-400">
                  Preferred hotel budget
                  <select
                    value={budgetLevel ?? ""}
                    onChange={(event) => {
                      manuallyChangedBudget.current = true;
                      setBudgetLevel(event.target.value ? Number(event.target.value) as BudgetLevel : undefined);
                    }}
                    className="mt-1.5 h-10 w-full rounded-xl border border-white/10 bg-[#171520] px-3 text-xs text-white outline-none [color-scheme:dark] focus:border-violet-400/60"
                  >
                    <option value="">Any budget</option>
                    {budgetLevelOptions.map((option) => (
                      <option key={option.level} value={option.level}>{option.level}. {option.label}</option>
                    ))}
                  </select>
                </label>

                <fieldset>
                  <legend className="mb-2 text-[10px] font-semibold uppercase tracking-[0.12em] text-slate-500">What do you want nearby?</legend>
                  <div className="flex flex-wrap gap-2">
                    {categories.map((category) => {
                      const selected = selectedCategories.includes(category);
                      return (
                        <button
                          key={category}
                          type="button"
                          aria-pressed={selected}
                          aria-label={category}
                          onClick={() => setSelectedCategories((current) =>
                            selected ? current.filter((item) => item !== category) : [...current, category])}
                          className={`rounded-full border px-3 py-2 text-[11px] transition ${selected ? "border-violet-400/50 bg-violet-500/20 text-violet-100" : "border-white/10 bg-white/[0.025] text-slate-400 hover:border-white/20"}`}
                        >
                          {category}
                        </button>
                      );
                    })}
                  </div>
                  {selectedCategories.length > 0 && (
                    <div className="mt-3 grid grid-cols-2 gap-2">
                      {selectedCategories.map((category) => (
                        <label key={category} className="text-[9px] text-slate-500">
                          {category} importance
                          <input
                            type="number"
                            min="0"
                            step="0.1"
                            placeholder="Equal"
                            value={categoryWeights[category] ?? ""}
                            onChange={(event) => setCategoryWeights((current) => ({
                              ...current,
                              [category]: event.target.value ? Number(event.target.value) : undefined,
                            }))}
                            className="mt-1 h-8 w-full rounded-lg border border-white/10 bg-[#171520] px-2 text-xs text-white outline-none focus:border-violet-400/60"
                          />
                        </label>
                      ))}
                    </div>
                  )}
                </fieldset>

                <fieldset>
                  <legend className="mb-2 text-[10px] font-semibold uppercase tracking-[0.12em] text-slate-500">Must-have amenities</legend>
                  <div className="flex flex-wrap gap-2">
                    {amenities.map((amenity) => {
                      const selected = selectedAmenities.includes(amenity);
                      return (
                        <button
                          key={amenity}
                          type="button"
                          aria-pressed={selected}
                          onClick={() => setSelectedAmenities((current) =>
                            selected ? current.filter((item) => item !== amenity) : [...current, amenity])}
                          className={`rounded-full border px-3 py-2 text-[11px] transition ${selected ? "border-fuchsia-400/50 bg-fuchsia-500/15 text-fuchsia-100" : "border-white/10 bg-white/[0.025] text-slate-400 hover:border-white/20"}`}
                        >
                          {amenity}
                        </button>
                      );
                    })}
                  </div>
                </fieldset>

                <button
                  type="button"
                  onClick={() => void runGuided()}
                  disabled={state.status === "loading"}
                  className="flex w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-violet-500 to-fuchsia-600 px-4 py-3 text-xs font-bold text-white shadow-lg shadow-violet-950/30 transition hover:brightness-110 disabled:cursor-wait disabled:opacity-60"
                >
                  <Sparkles className="h-4 w-4" /> Find my best stays
                </button>
              </div>
            )}

            {lastMessage && state.status !== "idle" && (
              <div className="mt-5 flex justify-end">
                <div className="max-w-[88%] rounded-2xl rounded-tr-md bg-gradient-to-br from-violet-600 to-purple-700 px-3.5 py-3 text-xs leading-5 text-white shadow-lg shadow-violet-950/20">
                  {lastMessage}
                </div>
              </div>
            )}

            {state.status === "loading" && <RecommendationLoading />}

            {state.status === "error" && (
              <div role="alert" className="mt-4 flex items-start gap-2.5">
                <div className="mt-0.5 grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-red-500/10 text-red-300">
                  <Bot className="h-3.5 w-3.5" />
                </div>
                <div className="rounded-2xl rounded-tl-md border border-red-400/15 bg-red-500/[0.06] px-3.5 py-3 text-xs text-red-200">
                  <p>{state.message}</p>
                  <button
                    type="button"
                    onClick={() => void retryRef.current?.()}
                    className="mt-2 inline-flex items-center gap-1.5 font-semibold text-red-100 underline underline-offset-2"
                  >
                    <RotateCcw className="h-3 w-3" /> Try again
                  </button>
                </div>
              </div>
            )}

            {state.status === "ready" && state.response.items.length === 0 && (
              <div className="mt-4 flex items-start gap-2.5">
                <div className="mt-0.5 grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-violet-500/15 text-violet-300"><Bot className="h-3.5 w-3.5" /></div>
                <p className="rounded-2xl rounded-tl-md border border-white/8 bg-white/[0.045] px-3.5 py-3 text-xs leading-5 text-slate-300">
                  I couldn’t find an eligible active stay for those preferences. Try a broader region, budget, or fewer requirements.
                </p>
              </div>
            )}

            {state.status === "ready" && state.response.items.length > 0 && (
              <div className="mt-4">
                <div className="mb-3 flex items-start gap-2.5">
                  <div className="mt-0.5 grid h-7 w-7 shrink-0 place-items-center rounded-lg bg-violet-500/15 text-violet-300"><Bot className="h-3.5 w-3.5" /></div>
                  <p className="rounded-2xl rounded-tl-md border border-white/8 bg-white/[0.045] px-3.5 py-3 text-xs leading-5 text-slate-300">
                    I found <strong className="text-white">{state.response.returnedCount} ranked match{state.response.returnedCount === 1 ? "" : "es"}</strong> from {state.response.evaluatedCandidates} evaluated and {state.response.totalMatchingCandidates ?? state.response.totalCandidates} eligible stays.
                  </p>
                </div>
                <div className="space-y-3">
                  {state.response.items.map((item) => (
                    <article key={item.hotelId} className="overflow-hidden rounded-2xl border border-white/10 bg-gradient-to-br from-white/[0.055] to-white/[0.02] p-3.5 transition hover:border-violet-400/25">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-violet-300">#{item.ranking} AI match</p>
                          <h3 className="mt-1 truncate text-sm font-bold text-white">{item.name}</h3>
                          <p className="mt-1 flex items-center gap-1 truncate text-[10px] text-slate-500">
                            <MapPin className="h-3 w-3 shrink-0" />
                            {item.region?.displayName || item.locationSummaryDescription || "Region unavailable"}
                          </p>
                        </div>
                        <div className="shrink-0 rounded-xl border border-violet-400/20 bg-violet-500/10 px-2.5 py-1.5 text-center">
                          <strong className="block text-sm text-violet-200">{item.finalScore.toFixed(0)}</strong>
                          <span className="text-[8px] uppercase text-violet-400">match</span>
                        </div>
                      </div>
                      <div className="mt-3 flex flex-wrap items-center gap-x-3 gap-y-1.5 text-[10px] text-slate-400">
                        <strong className="text-xs text-brand-gold">{formatUsdPrice(item.price)}</strong>
                        <span className="flex items-center gap-1"><Star className="h-3 w-3 fill-amber-400 text-amber-400" /> {item.rating ?? "—"} ({item.reviews ?? 0})</span>
                        {item.budgetLabel && <span>Level {item.budgetLevel} · {item.budgetLabel}</span>}
                      </div>
                      <p className="mt-3 text-[11px] leading-5 text-slate-300">{item.explanation.shortExplanation}</p>
                      <p className="mt-1 text-[9px] text-slate-600">
                        {item.explanation.isAiGenerated
                          ? "AI-written explanation · deterministic ranking"
                          : "Local explanation · deterministic ranking"}
                      </p>
                      <div className="mt-3 grid grid-cols-4 gap-1.5">
                        <Score label="Near" value={item.scores.interestProximityScore} />
                        <Score label="Budget" value={item.scores.budgetMatchScore} />
                        <Score label="Quality" value={item.scores.hotelQualityScore} />
                        <Score label="Amenities" value={item.scores.amenityMatchScore} />
                      </div>
                      {[...item.explanation.bestFor, ...item.matchedAmenities].length > 0 && (
                        <div className="mt-3 flex flex-wrap gap-1">
                          {[...new Set([...item.explanation.bestFor, ...item.matchedAmenities])].slice(0, 5).map((tag) => (
                            <span key={tag} className="rounded-full bg-white/5 px-2 py-1 text-[9px] text-slate-400">{tag}</span>
                          ))}
                        </div>
                      )}
                      {item.nearbyExperiences.length > 0 && (
                        <p className="mt-2 line-clamp-2 text-[10px] leading-4 text-slate-500">
                          Nearby: {item.nearbyExperiences.slice(0, 3).map((experience) =>
                            `${experience.name} (${experience.distanceKm.toFixed(1)} km)`).join(" · ")}
                        </p>
                      )}
                      {item.explanation.reasons.length > 0 && (
                        <details className="mt-2 text-[10px] text-slate-500">
                          <summary className="cursor-pointer select-none text-violet-300/80">Why this matches</summary>
                          <ul className="mt-1.5 list-disc space-y-1 pl-4">
                            {item.explanation.reasons.map((reason) => <li key={reason}>{reason}</li>)}
                          </ul>
                        </details>
                      )}
                      <button
                        type="button"
                        onClick={() => void openDetails(item.hotelId)}
                        disabled={detailsLoadingId === item.hotelId}
                        className="mt-3 flex w-full items-center justify-center gap-1.5 rounded-xl border border-violet-400/20 bg-violet-500/10 px-3 py-2.5 text-[11px] font-semibold text-violet-200 transition hover:bg-violet-500/20 disabled:opacity-60"
                      >
                        {detailsLoadingId === item.hotelId ? "Loading hotel details…" : "View hotel details"}
                        {detailsLoadingId !== item.hotelId && <ChevronRight className="h-3.5 w-3.5" />}
                      </button>
                    </article>
                  ))}
                </div>
              </div>
            )}
            <div ref={conversationEndRef} />
          </div>

          {mode === "natural" && (
            <footer className="border-t border-white/10 bg-[#0b0a11]/90 p-3">
              <div className="flex items-end gap-2 rounded-2xl border border-white/10 bg-[#171520] p-2 transition focus-within:border-violet-400/50 focus-within:ring-2 focus-within:ring-violet-500/10">
                <label className="sr-only" htmlFor="ai-stay-message">Describe the stay you want</label>
                <textarea
                  id="ai-stay-message"
                  rows={2}
                  value={naturalText}
                  maxLength={NATURAL_LANGUAGE_MAX_CHARACTERS}
                  onChange={(event) => setNaturalText(
                    event.target.value.slice(0, NATURAL_LANGUAGE_MAX_CHARACTERS),
                  )}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" && !event.shiftKey) {
                      event.preventDefault();
                      if (naturalText.trim() && state.status !== "loading") void runNatural(naturalText.trim());
                    }
                  }}
                  placeholder="Ask for a hotel…"
                  className="max-h-28 min-h-[2.75rem] flex-1 resize-none bg-transparent px-2 py-2 text-xs leading-5 text-white outline-none placeholder:text-slate-600"
                />
                <button
                  type="button"
                  onClick={() => void runNatural(naturalText.trim())}
                  disabled={!naturalText.trim() || state.status === "loading"}
                  aria-label="Send stay request"
                  className="grid h-11 w-11 shrink-0 place-items-center rounded-xl bg-gradient-to-br from-violet-500 to-fuchsia-600 text-white shadow-lg shadow-violet-950/40 transition hover:scale-[1.03] hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-35 disabled:hover:scale-100"
                >
                  <SendHorizontal className="h-4 w-4" />
                </button>
              </div>
              <div className="mt-1.5 flex items-center justify-between px-1 text-[9px] text-slate-600">
                <span>Enter to send · Shift+Enter for a new line</span>
                <span>{naturalText.length}/{NATURAL_LANGUAGE_MAX_CHARACTERS}</span>
              </div>
            </footer>
          )}
        </section>
      )}

      <button
        type="button"
        aria-label={open ? "Minimize AI stay assistant" : "Open AI stay assistant"}
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
        className={`${overlayLayers.assistant} group fixed bottom-5 right-5 grid h-14 w-14 place-items-center rounded-2xl bg-gradient-to-br from-violet-500 via-purple-600 to-fuchsia-600 text-white shadow-[0_12px_35px_rgba(124,58,237,.45)] transition hover:-translate-y-1 hover:shadow-[0_18px_45px_rgba(124,58,237,.55)] focus:outline-none focus:ring-2 focus:ring-violet-300 focus:ring-offset-2 focus:ring-offset-[#0b0c10]`}
      >
        <span className="absolute inset-0 rounded-2xl bg-white/20 opacity-0 transition group-hover:opacity-100" />
        {open ? <X className="relative h-5 w-5" /> : <Sparkles className="relative h-5 w-5" />}
        {!open && <span className="absolute -right-1 -top-1 h-3.5 w-3.5 animate-pulse rounded-full border-2 border-[#0b0c10] bg-emerald-400" />}
        <span className="pointer-events-none absolute right-[4.25rem] w-max rounded-lg border border-white/10 bg-[#171520] px-2.5 py-1.5 text-[10px] font-medium text-slate-200 opacity-0 shadow-xl transition group-hover:opacity-100">Ask Glinter AI</span>
      </button>
    </>
  );

  return createPortal(chatbot, document.body);
};

const RecommendationLoading = () => (
  <div role="status" aria-live="polite" className="mt-4 flex items-start gap-2.5">
    <div className="relative mt-0.5 grid h-8 w-8 shrink-0 place-items-center overflow-hidden rounded-xl bg-gradient-to-br from-violet-500 to-fuchsia-600 text-white">
      <span className="absolute inset-0 animate-pulse bg-white/20" />
      <Sparkles className="relative h-3.5 w-3.5 animate-pulse" />
    </div>
    <div className="min-w-0 flex-1 rounded-2xl rounded-tl-md border border-violet-400/15 bg-violet-500/[0.055] px-3.5 py-3">
      <div className="flex items-center gap-1.5">
        <span className="text-xs font-medium text-violet-100">Finding your best stays</span>
        {[0, 150, 300].map((delay) => (
          <span key={delay} className="h-1.5 w-1.5 animate-bounce rounded-full bg-violet-300" style={{ animationDelay: `${delay}ms` }} />
        ))}
      </div>
      <div className="mt-3 space-y-2">
        <div className="h-1.5 w-full animate-pulse overflow-hidden rounded-full bg-white/5">
          <div className="h-full w-2/3 animate-[pulse_1.2s_ease-in-out_infinite] rounded-full bg-gradient-to-r from-violet-500 to-fuchsia-500" />
        </div>
        <p className="text-[9px] text-slate-500">Comparing location, budget, quality and amenities…</p>
      </div>
    </div>
  </div>
);

const Score = ({ label, value }: { label: string; value?: number | null }) => (
  <div className="rounded-lg border border-white/5 bg-black/15 px-1.5 py-2 text-center">
    <strong className="block text-[10px] text-slate-200">{value == null ? "—" : `${Math.round(value * 100)}%`}</strong>
    <span className="mt-0.5 block text-[8px] text-slate-600">{label}</span>
  </div>
);

export default HotelRecommendationPanel;
