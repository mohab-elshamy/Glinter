import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  CalendarDays,
  CircleDollarSign,
  Compass,
  Crosshair,
  GripVertical,
  LoaderCircle,
  LocateFixed,
  MapPin,
  Navigation,
  Plus,
  RefreshCw,
  Route,
  Sparkles,
  Star,
  Trash2,
  Users,
  Wand2,
} from "lucide-react";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import LeafletMap, { type MapMarker, type MapRouteLine } from "@/components/LeafletMap";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import cairoImage from "@/assets/cairo.jpg";
import { formatExperiencePrice, formatUsdPrice } from "@/shared/lib/price";
import { experiencesApi } from "@/shared/services/api-experiences";
import { itinerariesApi } from "@/shared/services/api-itineraries";
import { regionsApi } from "@/shared/services/api-regions";
import { authStorage } from "@/shared/lib/auth";
import type { ItineraryPlanResponse, WeatherForecast } from "@/shared/types/itineraries";
import type {
  ExperienceCategory,
  ExperienceSummaryDto,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import { toast } from "sonner";
import {
  detectedRegionLabel,
  useCurrentLocation,
} from "@/features/where-to-stay/use-current-location";
import {
  buildFrontendItinerary,
  calculateDistanceKm,
  calculateKnownExperienceCost,
  countUnknownPrices,
  enumerateTripDates,
  getTimeSlots,
  moveItineraryItem,
  type ActivityLevel,
  type ItineraryItem,
} from "./planner";
import ExperienceRecommendationAssistant from "./ExperienceRecommendationAssistant";
import ExperienceDetailsModal from "@/features/local-buddies/ExperienceDetailsModal";
import type {
  ExperienceAvailabilityDto,
  ExperienceResponseDto,
  ExperienceReviewDto,
  ExperienceVisitInsightDto,
} from "@/shared/types/api";
import { useSearchParams } from "react-router-dom";
import { buildItineraryRouteLines } from "./itinerary-presentation";
import { useMyProfile } from "@/shared/hooks/use-my-profile";
import { mapInterestsToCategories } from "@/shared/lib/interest-mapping";

const categories: Array<{ value: ExperienceCategory; label: string }> = [
  { value: "Historical", label: "History & culture" },
  { value: "Nature", label: "Nature" },
  { value: "Shopping", label: "Shopping" },
  { value: "Nightlife", label: "Nightlife" },
  { value: "Dining", label: "Food & dining" },
];

const activityLevels: Array<{
  value: ActivityLevel;
  title: string;
  description: string;
}> = [
  { value: "Relaxed", title: "Relaxed", description: "Up to 2 activities per day" },
  { value: "Balanced", title: "Balanced", description: "Up to 3 activities per day" },
  { value: "Packed", title: "Packed", description: "Up to 4 activities per day" },
];

const toDateInput = (date: Date) => {
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 10);
};

const initialStart = () => {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  return toDateInput(date);
};

const initialEnd = () => {
  const date = new Date();
  date.setDate(date.getDate() + 3);
  return toDateInput(date);
};

const formatDay = (date: string) =>
  new Date(`${date}T12:00:00`).toLocaleDateString([], {
    weekday: "long",
    month: "short",
    day: "numeric",
  });

const formatDistance = (distanceKm?: number) => {
  if (distanceKm == null) return "Distance unavailable";
  if (distanceKm < 1) return `${Math.max(1, Math.round(distanceKm * 1_000))} m away`;
  return `${distanceKm.toFixed(distanceKm < 10 ? 1 : 0)} km away`;
};

const WhereToGo = () => {
  const [searchParams] = useSearchParams();
  const profileQuery = useMyProfile();
  const [region, setRegion] = useState<RegionHierarchyGids>({});
  const [startDate, setStartDate] = useState(initialStart);
  const [endDate, setEndDate] = useState(initialEnd);
  const [budget, setBudget] = useState("300");
  const [interests, setInterests] = useState<ExperienceCategory[]>(["Historical", "Nature"]);
  const [activityLevel, setActivityLevel] = useState<ActivityLevel>("Balanced");
  const [experiencePool, setExperiencePool] = useState<ExperienceSummaryDto[]>([]);
  const [itinerary, setItinerary] = useState<ItineraryItem[]>([]);
  const [loadState, setLoadState] = useState<LoadState>({ status: "ready" });
  const [selectedExperienceId, setSelectedExperienceId] = useState<number>();
  const [addSelections, setAddSelections] = useState<Record<string, string>>({});
  const [prioritizeNearest, setPrioritizeNearest] = useState(false);
  const [recenterBump, setRecenterBump] = useState(0);
  const [plannerMode, setPlannerMode] = useState<"guided" | "natural">("guided");
  const [naturalRequest, setNaturalRequest] = useState("");
  const [planSource, setPlanSource] = useState<"ai" | "local">();
  const [savedItineraryId, setSavedItineraryId] = useState<string>();
  const [weather, setWeather] = useState<WeatherForecast>();
  const [backendPlan, setBackendPlan] = useState<ItineraryPlanResponse>();
  const [customStartLatitude, setCustomStartLatitude] = useState("");
  const [customStartLongitude, setCustomStartLongitude] = useState("");
  const [details, setDetails] = useState<{
    experience: ExperienceResponseDto;
    availability: ExperienceAvailabilityDto[];
    reviews: ExperienceReviewDto[];
    insight?: ExperienceVisitInsightDto;
  }>();
  const currentLocation = useCurrentLocation();
  const currentPosition = currentLocation.state.status === "ready"
    ? currentLocation.state.position
    : undefined;
  const currentRegionLabel = detectedRegionLabel(currentLocation.state.detectedRegion);
  const customStartPoint = useMemo(() => {
    if (customStartLatitude === "" || customStartLongitude === "") return undefined;
    const latitude = Number(customStartLatitude);
    const longitude = Number(customStartLongitude);
    if (
      !Number.isFinite(latitude) ||
      !Number.isFinite(longitude) ||
      latitude < -90 ||
      latitude > 90 ||
      longitude < -180 ||
      longitude > 180
    ) {
      return undefined;
    }

    return {
      latitude,
      longitude,
    };
  }, [customStartLatitude, customStartLongitude]);
  const planningStartPosition = customStartPoint ?? currentPosition;
  const startPickerCenter = useMemo<[number, number]>(() => {
    if (customStartPoint) return [customStartPoint.latitude, customStartPoint.longitude];
    if (currentPosition) return [currentPosition.latitude, currentPosition.longitude];
    return [26.8206, 30.8025];
  }, [currentPosition, customStartPoint]);
  const startPickerMarker = useMemo<MapMarker | null>(() => {
    if (!customStartPoint) return null;

    return {
      lat: customStartPoint.latitude,
      lng: customStartPoint.longitude,
      name: "Selected start point",
      label: "Start",
      color: "#38bdf8",
    };
  }, [customStartPoint]);
  const setCustomStartPoint = useCallback((latitude: number, longitude: number) => {
    setCustomStartLatitude(latitude.toFixed(6));
    setCustomStartLongitude(longitude.toFixed(6));
  }, []);
  const clearCustomStartPoint = useCallback(() => {
    setCustomStartLatitude("");
    setCustomStartLongitude("");
  }, []);

  useEffect(() => {
    if (profileQuery.data?.profileType !== "Traveler") return;
    const mapped = mapInterestsToCategories(
      profileQuery.data.interests.map((interest) => interest.name),
    );
    if (mapped.length > 0) setInterests(mapped);
  }, [profileQuery.data]);

  const tripDates = useMemo(() => enumerateTripDates(startDate, endDate), [startDate, endDate]);
  const knownCost = useMemo(() => calculateKnownExperienceCost(itinerary), [itinerary]);
  const unknownPriceCount = useMemo(() => countUnknownPrices(itinerary), [itinerary]);
  const budgetValue = Number(budget) > 0 ? Number(budget) : undefined;
  const usedIds = useMemo(() => new Set(itinerary.map((item) => item.experience.id)), [itinerary]);
  const unusedExperiences = useMemo(
    () => experiencePool.filter((experience) => !usedIds.has(experience.id)),
    [experiencePool, usedIds],
  );
  const sortedUnusedExperiences = useMemo(() => {
    if (!prioritizeNearest || !planningStartPosition) return unusedExperiences;
    return [...unusedExperiences].sort((left, right) => {
      const leftDistance = calculateDistanceKm(planningStartPosition, left);
      const rightDistance = calculateDistanceKm(planningStartPosition, right);
      if (leftDistance == null) return rightDistance == null ? 0 : 1;
      if (rightDistance == null) return -1;
      return leftDistance - rightDistance;
    });
  }, [planningStartPosition, prioritizeNearest, unusedExperiences]);

  const itineraryByDay = useMemo(
    () => tripDates.map((date) => ({
      date,
      items: itinerary.filter((item) => item.date === date),
    })),
    [itinerary, tripDates],
  );

  const markers: MapMarker[] = useMemo(
    () => itinerary.flatMap((item) => {
      const { latitude, longitude } = item.experience;
      if (latitude == null || longitude == null) return [];
      const dayNumber = tripDates.indexOf(item.date) + 1;
      const position = itinerary.filter((entry) => entry.date === item.date)
        .findIndex((entry) => entry.experience.id === item.experience.id) + 1;
      return [{
        id: item.experience.id,
        lat: latitude,
        lng: longitude,
        name: `${dayNumber}.${position} ${item.experience.name}`,
        cheapestPrice: item.experience.startingPricePerPerson,
        label: `${dayNumber}.${position}`,
        data: {
          rating: item.experience.rating ?? 0,
          area: `${formatDay(item.date)} · ${item.time}${
            planningStartPosition
              ? ` · ${formatDistance(calculateDistanceKm(planningStartPosition, item.experience))}`
              : ""
          }`,
        },
      }];
    }),
    [itinerary, planningStartPosition, tripDates],
  );

  const routeLines: MapRouteLine[] = useMemo(
    () => buildItineraryRouteLines(backendPlan),
    [backendPlan],
  );

  const mapCenter = useMemo<[number, number]>(() => {
    if (markers.length === 0) return [26.8206, 30.8025];
    return [
      markers.reduce((sum, marker) => sum + marker.lat, 0) / markers.length,
      markers.reduce((sum, marker) => sum + marker.lng, 0) / markers.length,
    ];
  }, [markers]);

  const selectedMarker = markers.find((marker) => marker.id === selectedExperienceId) ?? null;
  const activitiesWithoutCoordinates = planningStartPosition
    ? itinerary.filter(
      (item) => item.experience.latitude == null || item.experience.longitude == null,
    ).length
    : 0;

  const toggleInterest = (category: ExperienceCategory) => {
    setInterests((current) => current.includes(category)
      ? current.filter((item) => item !== category)
      : [...current, category]);
  };

  const buildPlan = async () => {
    if (tripDates.length === 0) {
      toast.error("Choose a valid start and end date.");
      return;
    }
    if (new Date(`${endDate}T00:00:00`) < new Date(`${startDate}T00:00:00`)) {
      toast.error("The end date must be on or after the start date.");
      return;
    }

    setLoadState({ status: "loading" });
    setSavedItineraryId(undefined);
    try {
      const filters = {
        adm0Gid: region.adm0Gid,
        adm1Gid: region.adm1Gid,
        adm2Gid: region.adm2Gid,
        adm3Gid: region.adm3Gid,
        sortBy: prioritizeNearest && planningStartPosition ? "Distance" : "Recommended",
        sortDirection: prioritizeNearest && planningStartPosition ? "Asc" : "Desc",
        currentLatitude: prioritizeNearest ? planningStartPosition?.latitude : undefined,
        currentLongitude: prioritizeNearest ? planningStartPosition?.longitude : undefined,
        page: 1,
        pageSize: 100,
      } as const;
      const categoryRequests = interests.length > 0 ? interests : [undefined];
      const results = await Promise.all(categoryRequests.map((category) =>
        experiencesApi.getExperiences({ ...filters, category })));
      const uniqueExperiences = new Map<number, ExperienceSummaryDto>();
      results.flatMap((result) => result.items).forEach((experience) => {
        if (experience.isActive) uniqueExperiences.set(experience.id, experience);
      });
      const approved = [...uniqueExperiences.values()];
      const hasSelectedRegion = Object.values(region).some((value) => value != null);
      let origin = customStartPoint
        ? {
            latitude: customStartPoint.latitude,
            longitude: customStartPoint.longitude,
            label: "Pinned start point",
          }
        : currentPosition
        ? {
            latitude: currentPosition.latitude,
            longitude: currentPosition.longitude,
            label: "Current browser location",
          }
        : undefined;
      if (!origin && hasSelectedRegion) {
        const centroid = await regionsApi.getCentroid(region);
        origin = {
          latitude: centroid.latitude,
          longitude: centroid.longitude,
          label: `${centroid.name || "Selected region"} centroid (${centroid.administrativeLevel})`,
        };
      }
      if (!origin && plannerMode === "guided") {
        throw new Error("Select a destination or allow browser location before planning.");
      }

      const common = {
        start: origin ?? { latitude: 0, longitude: 0 },
        origin,
        date: startDate,
        startDate,
        endDate,
        destination: currentRegionLabel || undefined,
        dayStartLocal: "09:00:00",
        dayEndLocal: "21:00:00",
        travelMode: "PublicTransit" as const,
        fallbackTravelMode: "Walking" as const,
        pace: activityLevel,
        adm0Gid: region.adm0Gid,
        adm1Gid: region.adm1Gid,
        adm2Gid: region.adm2Gid,
        adm3Gid: region.adm3Gid,
        maxStops: activityLevel === "Relaxed" ? 2 : activityLevel === "Packed" ? 4 : 3,
        candidateLimit: 30,
        guestsCount: 1,
        includeMealBreaks: interests.includes("Dining"),
        returnToStart: true,
        avoidLongWalking: activityLevel === "Relaxed",
        preferredLanguage: "en" as const,
        totalBudget: budgetValue,
        currency: "USD",
      };
      const plan = plannerMode === "natural"
        ? (await itinerariesApi.planNaturalLanguage({
            ...common,
            text: naturalRequest.trim(),
          })).itinerary
        : await itinerariesApi.plan({
            ...common,
            categories: interests.map((category) => ({ category })),
          });
      const planDays = plan.days.length > 0
        ? plan.days
        : [{ date: plan.date, stops: plan.stops }];
      const ids = [...new Set(planDays.flatMap((day) =>
        day.stops.flatMap((stop) => stop.experienceId ?? [])))];
      const fullExperiences = await Promise.all(ids.map((id) =>
        experiencesApi.getExperienceById(id)));
      const byId = new Map(fullExperiences.map((experience) => [experience.id, experience]));
      const next: ItineraryItem[] = planDays.flatMap((day) =>
        day.stops.flatMap((stop) => {
          if (!stop.experienceId) return [];
          const experience = byId.get(stop.experienceId);
          return experience
            ? [{
                experience,
                date: day.date,
                time: stop.arrivalLocal.slice(0, 5),
              }]
            : [];
        }));
      setExperiencePool(approved);
      setItinerary(next);
      setBackendPlan(plan);
      setPlanSource("ai");
      setSelectedExperienceId(next[0]?.experience.id);
      setLoadState({ status: "ready" });

      itinerariesApi.weather({
        latitude: plan.origin.latitude,
        longitude: plan.origin.longitude,
        location: currentRegionLabel || "Trip area",
        startDate,
        endDate,
      }).then(setWeather).catch(() => setWeather(undefined));

      if (next.length === 0) {
        toast.error("No matching backend experiences were found. Try broader interests, region or budget.");
      } else {
        toast.success(`Built ${plan.days.length || 1} days in one backend planning request.`);
      }
    } catch (error) {
      try {
        const fallbackResult = await experiencesApi.getExperiences({
          adm0Gid: region.adm0Gid,
          adm1Gid: region.adm1Gid,
          adm2Gid: region.adm2Gid,
          adm3Gid: region.adm3Gid,
          page: 1,
          pageSize: 100,
        });
        const fallback = buildFrontendItinerary(fallbackResult.items, {
          startDate,
          endDate,
          budget: budgetValue,
          interests,
          activityLevel,
          prioritizeNearest,
          currentPosition: planningStartPosition,
        });
        setExperiencePool(fallbackResult.items);
        setItinerary(fallback);
        setBackendPlan(undefined);
        setPlanSource("local");
        setLoadState({ status: "ready" });
        toast.warning("The backend planner was unavailable. A local fallback draft is shown.");
      } catch {
        const message = error instanceof Error ? error.message : "Could not build the itinerary.";
        setLoadState({ status: "error", message });
        toast.error(message);
      }
    }
  };

  const saveItinerary = async () => {
    if (!authStorage.isAuthenticated()) {
      toast.error("Sign in as a traveler to save this itinerary.");
      return;
    }
    if ((customStartLatitude === "") !== (customStartLongitude === "") || (
      customStartLatitude !== "" && !customStartPoint
    )) {
      toast.error("Select a valid pinned start point or clear the pin.");
      return;
    }
    try {
      const saved = await itinerariesApi.save({
        title: `${currentRegionLabel || "Egypt"} itinerary`,
        destination: currentRegionLabel || "Egypt",
        ...region,
        startDate,
        endDate,
        preferredLanguage: "en",
        estimatedTotalCost: knownCost || undefined,
        currency: "USD",
        plannerExplanation: backendPlan?.explanation.summary,
        warnings: backendPlan?.warnings,
        recommendationScore: backendPlan?.score,
        totalDistanceKm: backendPlan?.totalDistanceKm,
        totalTravelMinutes: backendPlan?.totalTravelMinutes,
        pace: backendPlan?.pace,
        travelMode: backendPlan?.travelMode,
        fallbackTravelMode: backendPlan?.fallbackTravelMode,
        origin: backendPlan?.origin,
        weatherLatitude: backendPlan?.origin.latitude,
        weatherLongitude: backendPlan?.origin.longitude,
        weatherLocation: backendPlan?.destination || currentRegionLabel,
        items: itinerary.map((item) => {
          const dayNumber = Math.max(1, tripDates.indexOf(item.date) + 1);
          const backendDay = backendPlan?.days.find((day) => day.dayNumber === dayNumber);
          const backendStop = backendDay?.stops.find((stop) => stop.experienceId === item.experience.id);
          const incomingLeg = backendDay?.legs.find((leg) => leg.toOrder === backendStop?.order);
          return {
          dayNumber,
          sortOrder: itinerary
            .filter((entry) => entry.date === item.date)
            .findIndex((entry) => entry.experience.id === item.experience.id) + 1,
          entityType: "Experience",
          entityId: item.experience.id,
          name: item.experience.name,
          latitude: item.experience.latitude ?? 0,
          longitude: item.experience.longitude ?? 0,
          startTime: `${item.time}:00`,
          endTime: backendStop?.departureLocal ? `${backendStop.departureLocal}:00` : undefined,
          estimatedDurationMinutes: backendStop?.durationMinutes,
          estimatedCost: item.experience.startingPricePerPerson,
          explanation: backendStop?.explanation,
          category: item.experience.category,
          rating: item.experience.rating,
          imageUrl: item.experience.featuredImages[0]?.link,
          travelModeFromPrevious: incomingLeg?.mode,
          routeProviderFromPrevious: incomingLeg?.provider,
          routeGeometryFromPrevious: incomingLeg?.geometry,
          routeInstructionsFromPrevious: incomingLeg?.steps,
          routeWarningsFromPrevious: incomingLeg?.warnings,
          distanceKmFromPrevious: incomingLeg?.distanceKm,
          travelDurationMinutesFromPrevious: incomingLeg?.durationMinutes,
        };
        }),
      });
      setSavedItineraryId(saved.id);
      toast.success("Itinerary saved to My Trips.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not save the itinerary.");
    }
  };

  const openExperienceDetails = useCallback(async (id: number) => {
    try {
      const [experience, availability, reviews, insight] = await Promise.all([
        experiencesApi.getExperienceById(id),
        experiencesApi.getAvailability(id),
        experiencesApi.getReviews(id, 1, 5),
        experiencesApi.getVisitInsights(id).catch(() => undefined),
      ]);
      setDetails({ experience, availability, reviews, insight });
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not load experience details.");
    }
  }, []);

  useEffect(() => {
    const experienceId = Number(searchParams.get("experienceId"));
    if (Number.isInteger(experienceId) && experienceId > 0) {
      void openExperienceDetails(experienceId);
    }
  }, [openExperienceDetails, searchParams]);

  const removeActivity = (experienceId: number) => {
    setItinerary((current) => current.filter((item) => item.experience.id !== experienceId));
    if (selectedExperienceId === experienceId) setSelectedExperienceId(undefined);
  };

  const replaceActivity = (experienceId: number) => {
    const currentItem = itinerary.find((item) => item.experience.id === experienceId);
    if (!currentItem) return;
    const replacement = sortedUnusedExperiences.find(
      (experience) => experience.category === currentItem.experience.category,
    ) ?? sortedUnusedExperiences[0];
    if (!replacement) {
      toast.error("There are no unused experiences available for replacement.");
      return;
    }
    setSelectedExperienceId(replacement.id);
    setItinerary((current) => current.map((item) => item.experience.id === experienceId
      ? { ...item, experience: replacement }
      : item));
  };

  const addActivity = (date: string) => {
    const id = Number(addSelections[date]);
    const experience = sortedUnusedExperiences.find((item) => item.id === id);
    if (!experience) return;
    const itemsForDay = itinerary.filter((item) => item.date === date);
    const slots = getTimeSlots(activityLevel);
    const time = slots[itemsForDay.length] ?? `${Math.min(21, 10 + itemsForDay.length * 3)}:00`;
    setItinerary((current) => [...current, { experience, date, time }]);
    setSelectedExperienceId(experience.id);
    setAddSelections((current) => ({ ...current, [date]: "" }));
  };

  const addRecommendedExperience = useCallback(async (id: number) => {
    if (itinerary.some((item) => item.experience.id === id)) {
      toast.info("That experience is already in this itinerary.");
      return;
    }
    const experience = await experiencesApi.getExperienceById(id);
    const date = tripDates[0] ?? startDate;
    const count = itinerary.filter((item) => item.date === date).length;
    const time = getTimeSlots(activityLevel)[count] ?? "18:00";
    setExperiencePool((current) => current.some((item) => item.id === id) ? current : [...current, experience]);
    setItinerary((current) => [...current, { experience, date, time }]);
    toast.success(`${experience.name} added to Day 1.`);
  }, [activityLevel, itinerary, startDate, tripDates]);

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-7xl px-4 py-8 sm:py-12">
        <section className="relative overflow-hidden rounded-3xl border border-white/10 bg-gradient-to-br from-primary/20 via-background to-amber-500/10 p-6 sm:p-10">
          <div className="absolute -right-16 -top-20 h-64 w-64 rounded-full bg-primary/20 blur-3xl" />
          <div className="relative max-w-3xl">
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-primary/25 bg-primary/10 px-3 py-1.5 text-xs font-semibold text-primary">
              <Compass className="h-4 w-4" /> Backend itinerary planner
            </div>
            <h1 className="text-3xl font-extrabold leading-tight sm:text-5xl">
              Shape your Egypt trip, <span className="text-gradient-orange">one day at a time</span>
            </h1>
            <p className="mt-4 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">
              Choose your destination and travel style, then arrange real Glinter experiences into a practical itinerary.
            </p>
            <p className="mt-3 text-xs text-muted-foreground">
              The backend planner ranks real approved experiences and builds the route. If it is unavailable, the page labels its local fallback clearly.
            </p>
          </div>
        </section>

        <div className="mt-6 grid gap-6 xl:grid-cols-[23rem_minmax(0,1fr)]">
          <aside className="self-start rounded-2xl border border-border bg-card p-5 xl:sticky xl:top-4">
            <div className="flex items-center gap-2">
              <Wand2 className="h-5 w-5 text-accent" />
              <h2 className="text-lg font-bold">Plan preferences</h2>
            </div>

            <div className="mt-4 grid grid-cols-2 gap-2 rounded-xl bg-secondary p-1">
              {(["guided", "natural"] as const).map((mode) => (
                <button
                  key={mode}
                  type="button"
                  aria-pressed={plannerMode === mode}
                  onClick={() => setPlannerMode(mode)}
                  className={`rounded-lg px-3 py-2 text-xs font-semibold ${
                    plannerMode === mode ? "bg-primary text-primary-foreground" : "text-muted-foreground"
                  }`}
                >
                  {mode === "guided" ? "Guided plan" : "Describe your trip"}
                </button>
              ))}
            </div>

            {plannerMode === "natural" && (
              <label className="mt-4 block text-xs font-medium">
                What should your trip feel like?
                <textarea
                  value={naturalRequest}
                  maxLength={1500}
                  onChange={(event) => setNaturalRequest(event.target.value)}
                  placeholder="I will be in Cairo and prefer history, local food, a relaxed pace, and little walking."
                  className="mt-1 min-h-28 w-full resize-y rounded-xl border border-border bg-secondary p-3 text-sm"
                />
                <span className="mt-1 block text-right text-[10px] text-muted-foreground">
                  {naturalRequest.length}/1500
                </span>
              </label>
            )}

            <div className="mt-5">
              <RegionCascadeSelect
                value={region}
                onChange={setRegion}
                label="Destination"
              />
              <p className="mt-2 text-[11px] text-muted-foreground">
                Leave broader levels on “All” to explore more of Egypt.
              </p>
            </div>

            <div className="mt-5 grid grid-cols-2 gap-3">
              <label className="text-xs font-medium">
                Start date
                <input
                  type="date"
                  value={startDate}
                  onChange={(event) => setStartDate(event.target.value)}
                  className="mt-1 w-full rounded-lg border border-border bg-secondary px-3 py-2"
                />
              </label>
              <label className="text-xs font-medium">
                End date
                <input
                  type="date"
                  min={startDate}
                  value={endDate}
                  onChange={(event) => setEndDate(event.target.value)}
                  className="mt-1 w-full rounded-lg border border-border bg-secondary px-3 py-2"
                />
              </label>
            </div>

            <label className="mt-5 block text-xs font-medium">
              Total trip experience budget (USD)
              <div className="relative mt-1">
                <CircleDollarSign className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                <input
                  type="number"
                  min="0"
                  value={budget}
                  onChange={(event) => setBudget(event.target.value)}
                  className="w-full rounded-lg border border-border bg-secondary py-2 pl-9 pr-3"
                  placeholder="No limit"
                />
              </div>
            </label>

            <section className="mt-5 rounded-xl border border-sky-500/20 bg-sky-500/5 p-3">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h3 className="text-xs font-semibold">Trip start location</h3>
                  <p className="mt-1 text-[11px] text-muted-foreground">
                    Used only for map context and optional distance ordering.
                  </p>
                </div>
                <button
                  type="button"
                  onClick={currentLocation.locate}
                  disabled={currentLocation.state.status === "locating"}
                  className="flex shrink-0 items-center gap-1.5 rounded-lg border border-sky-400/30 bg-sky-500/10 px-3 py-2 text-xs font-semibold text-sky-300 disabled:cursor-wait disabled:opacity-60"
                >
                  <LocateFixed className={`h-3.5 w-3.5 ${
                    currentLocation.state.status === "locating" ? "animate-pulse" : ""
                  }`} />
                  {currentLocation.state.status === "locating" ? "Locating…" : "Locate me"}
                </button>
              </div>
              {currentPosition && (
                <p className="mt-3 text-[11px] text-sky-200">
                  Location ready · accuracy ±{Math.round(currentPosition.accuracy)} m
                  {currentRegionLabel && <> · {currentRegionLabel}</>}
                </p>
              )}
              {["denied", "unavailable", "timeout", "insecure", "unsupported"].includes(
                currentLocation.state.status,
              ) && (
                <p role="alert" className="mt-3 text-[11px] text-amber-300">
                  {currentLocation.state.regionMessage}
                </p>
              )}
              <div className="mt-3 overflow-hidden rounded-xl border border-border bg-background">
                <LeafletMap
                  center={startPickerCenter}
                  zoom={customStartPoint || currentPosition ? 13 : 6}
                  markers={startPickerMarker ? [startPickerMarker] : []}
                  selectedMarker={startPickerMarker}
                  onMapClick={setCustomStartPoint}
                  currentLocation={currentPosition}
                  showLegend={false}
                  showFullscreen={false}
                  showSearch
                  height="14rem"
                />
              </div>
              <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-[10px] text-muted-foreground">
                <span>
                  {customStartPoint
                    ? `Pinned start: ${customStartPoint.latitude.toFixed(5)}, ${customStartPoint.longitude.toFixed(5)}`
                    : "Start priority: browser location, then selected region centroid."}
                </span>
                {customStartPoint && (
                  <button
                    type="button"
                    onClick={clearCustomStartPoint}
                    className="inline-flex items-center gap-1 rounded-lg border border-border px-2 py-1 text-[10px] text-foreground hover:bg-secondary"
                  >
                    <Trash2 className="h-3 w-3" /> Clear pin
                  </button>
                )}
              </div>
              <label className={`mt-3 flex items-start gap-2 text-xs ${
                planningStartPosition ? "cursor-pointer" : "cursor-not-allowed text-muted-foreground"
              }`}>
                <input
                  type="checkbox"
                  checked={prioritizeNearest}
                  disabled={!planningStartPosition}
                  onChange={(event) => setPrioritizeNearest(event.target.checked)}
                  className="mt-0.5"
                />
                <span>
                  <span className="block font-medium">Prioritize nearby experiences</span>
                  <span className="text-[10px] text-muted-foreground">
                    Rebuild the draft to apply nearest-first ordering.
                  </span>
                </span>
              </label>
            </section>

            <fieldset className="mt-5">
              <legend className="text-xs font-medium">Interests</legend>
              <div className="mt-2 flex flex-wrap gap-2">
                {categories.map((category) => {
                  const selected = interests.includes(category.value);
                  return (
                    <button
                      type="button"
                      key={category.value}
                      aria-pressed={selected}
                      onClick={() => toggleInterest(category.value)}
                      className={`rounded-full border px-3 py-1.5 text-xs transition ${
                        selected
                          ? "border-accent bg-accent/15 text-accent"
                          : "border-border text-muted-foreground hover:bg-secondary"
                      }`}
                    >
                      {category.label}
                    </button>
                  );
                })}
              </div>
            </fieldset>

            <fieldset className="mt-5">
              <legend className="text-xs font-medium">Preferred activity level</legend>
              <div className="mt-2 space-y-2">
                {activityLevels.map((level) => (
                  <label
                    key={level.value}
                    className={`flex cursor-pointer items-start gap-3 rounded-xl border p-3 ${
                      activityLevel === level.value ? "border-accent bg-accent/10" : "border-border"
                    }`}
                  >
                    <input
                      type="radio"
                      name="activity-level"
                      value={level.value}
                      checked={activityLevel === level.value}
                      onChange={() => setActivityLevel(level.value)}
                      className="mt-1"
                    />
                    <span>
                      <span className="block text-sm font-semibold">{level.title}</span>
                      <span className="text-[11px] text-muted-foreground">{level.description}</span>
                    </span>
                  </label>
                ))}
              </div>
            </fieldset>

            <button
              type="button"
              disabled={loadState.status === "loading"}
              onClick={() => void buildPlan()}
              className="btn-accent mt-6 flex w-full items-center justify-center gap-2 rounded-xl py-3 text-sm font-semibold disabled:opacity-50"
            >
              {loadState.status === "loading"
                ? <><LoaderCircle className="h-4 w-4 animate-spin" /> Planning your route…</>
                : <><Sparkles className="h-4 w-4" /> Build my itinerary</>}
            </button>
            {loadState.status === "error" && (
              <p role="alert" className="mt-3 text-xs text-destructive">{loadState.message}</p>
            )}
          </aside>

          <div className="min-w-0 space-y-6">
            {itinerary.length === 0 ? (
              <section className="grid min-h-[32rem] place-items-center rounded-2xl border border-dashed border-border bg-card/40 p-8 text-center">
                <div className="max-w-md">
                  <div className="mx-auto grid h-16 w-16 place-items-center rounded-2xl bg-primary/10">
                    <Route className="h-8 w-8 text-primary" />
                  </div>
                  <h2 className="mt-5 text-2xl font-bold">Your route starts here</h2>
                  <p className="mt-3 text-sm leading-6 text-muted-foreground">
                    Set your dates, budget and interests. The planner will load real backend experiences and distribute them across your trip.
                  </p>
                  <div className="mt-6 grid gap-3 text-left sm:grid-cols-3">
                    <EmptyFeature icon={CalendarDays} label="Day-by-day" />
                    <EmptyFeature icon={MapPin} label="Mapped stops" />
                    <EmptyFeature icon={CircleDollarSign} label="Known costs" />
                  </div>
                </div>
              </section>
            ) : (
              <>
                <div className={`rounded-xl border px-4 py-3 text-xs ${
                  planSource === "ai"
                    ? "border-primary/25 bg-primary/10 text-primary"
                    : "border-amber-500/25 bg-amber-500/10 text-amber-300"
                }`}>
                  {planSource === "ai"
                    ? "Built by the backend itinerary planner from eligible Glinter experiences."
                    : "Local fallback draft — backend AI/routing was unavailable."}
                </div>

                {weather && (
                  <section className="rounded-2xl border border-border bg-card p-4">
                    <div className="flex items-center justify-between gap-3">
                      <h2 className="font-bold">Trip weather</h2>
                      {weather.providerDataTimestampUtc && (
                        <span className="text-[10px] text-muted-foreground">
                          Updated {new Date(weather.providerDataTimestampUtc).toLocaleTimeString()}
                        </span>
                      )}
                    </div>
                    {weather.isAvailable ? (
                      <div className="mt-3 flex snap-x gap-3 overflow-x-auto pb-2">
                        {weather.days.map((day) => (
                          <article key={day.date} className="min-w-44 snap-start rounded-xl bg-secondary p-3 text-xs">
                            <p className="font-semibold">{formatDay(day.date)}</p>
                            <p className="mt-2 text-lg font-bold">
                              {day.temperatureMinC?.toFixed(0)}°–{day.temperatureMaxC?.toFixed(0)}°C
                            </p>
                            <p className="text-muted-foreground">{day.condition}</p>
                            <p className="mt-2">{day.precipitationProbabilityPercent ?? "—"}% rain · {day.windSpeedKph?.toFixed(0) ?? "—"} km/h wind</p>
                            <p className="mt-2 text-muted-foreground">{day.advice}</p>
                          </article>
                        ))}
                      </div>
                    ) : (
                      <p className="mt-2 text-sm text-muted-foreground">{weather.unavailableReason}</p>
                    )}
                  </section>
                )}

                <section className="grid gap-3 sm:grid-cols-4">
                  <SummaryCard label="Trip length" value={`${tripDates.length} days`} icon={CalendarDays} />
                  <SummaryCard label="Activities" value={String(itinerary.length)} icon={Route} />
                  <SummaryCard
                    label="Known cost"
                    value={knownCost === 0 && unknownPriceCount > 0 ? "No priced items" : formatUsdPrice(knownCost)}
                    icon={CircleDollarSign}
                  />
                  <SummaryCard
                    label="Budget status"
                    value={backendPlan?.budgetApplied
                      ? backendPlan.isWithinBudget ? "Within budget" : "Over budget"
                      : budgetValue == null ? "No limit" : formatUsdPrice(Math.max(0, budgetValue - knownCost))}
                    icon={Compass}
                  />
                  {backendPlan && (
                    <>
                      <SummaryCard label="Pace" value={backendPlan.pace} icon={Compass} />
                      <SummaryCard label="Transport" value={backendPlan.travelMode} icon={Navigation} />
                      <SummaryCard label="Route distance" value={`${backendPlan.totalDistanceKm.toFixed(1)} km`} icon={Route} />
                      <SummaryCard label="Travel time" value={`${backendPlan.totalTravelMinutes} min`} icon={CalendarDays} />
                      <SummaryCard label="Recommendation" value={`${backendPlan.score.toFixed(0)}/100`} icon={Star} />
                    </>
                  )}
                </section>

                {backendPlan && (
                  <section className="rounded-2xl border border-border bg-card p-5">
                    <h2 className="font-bold">{backendPlan.destination || "Trip"} overview</h2>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {backendPlan.startDate} – {backendPlan.endDate} · Start: {backendPlan.originSource}
                    </p>
                    <p className="mt-3 text-sm">{backendPlan.explanation.summary}</p>
                    {backendPlan.explanation.reasons.length > 0 && (
                      <ul className="mt-3 list-disc space-y-1 pl-5 text-xs text-muted-foreground">
                        {backendPlan.explanation.reasons.map((reason) => <li key={reason}>{reason}</li>)}
                      </ul>
                    )}
                    {backendPlan.warnings.length > 0 && (
                      <div className="mt-4 space-y-2" aria-label="Itinerary warnings">
                        {backendPlan.warnings.map((warning) => (
                          <p key={warning} className="rounded-lg border border-amber-500/25 bg-amber-500/10 px-3 py-2 text-xs text-amber-300">{warning}</p>
                        ))}
                      </div>
                    )}
                  </section>
                )}

                {unknownPriceCount > 0 && (
                  <p className="rounded-xl border border-amber-500/25 bg-amber-500/10 px-4 py-3 text-xs text-amber-300">
                    {unknownPriceCount} {unknownPriceCount === 1 ? "activity has" : "activities have"} no backend price and therefore {unknownPriceCount === 1 ? "is" : "are"} excluded from the known total.
                  </p>
                )}

                <section className="overflow-hidden rounded-2xl border border-border bg-card">
                  <div className="flex flex-col justify-between gap-2 border-b border-border p-4 sm:flex-row sm:items-center">
                    <div>
                      <h2 className="font-bold">Interactive route map</h2>
                      <p className="text-xs text-muted-foreground">Select a marker to highlight its itinerary activity.</p>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs text-muted-foreground">{markers.length} mapped stops</span>
                      <button
                        type="button"
                        onClick={currentLocation.locate}
                        disabled={currentLocation.state.status === "locating"}
                        className="flex items-center gap-1.5 rounded-lg border border-sky-400/30 px-3 py-2 text-xs text-sky-300 disabled:opacity-50"
                      >
                        <LocateFixed className="h-3.5 w-3.5" />
                        {currentLocation.state.status === "locating" ? "Locating…" : "Locate me"}
                      </button>
                      <button
                        type="button"
                        disabled={!currentPosition}
                        onClick={() => setRecenterBump((value) => value + 1)}
                        className="flex items-center gap-1.5 rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-40"
                      >
                        <Crosshair className="h-3.5 w-3.5" /> Recenter
                      </button>
                    </div>
                  </div>
                  {currentPosition && (
                    <div className="flex flex-wrap justify-between gap-2 border-b border-sky-500/20 bg-sky-500/10 px-4 py-3 text-xs text-sky-200">
                      <span>
                        <strong>You are here</strong> · accuracy ±{Math.round(currentPosition.accuracy)} m
                        {currentRegionLabel && <> · {currentRegionLabel}</>}
                      </span>
                      <span>{currentLocation.state.regionMessage || "Destination filters remain unchanged."}</span>
                    </div>
                  )}
                  {activitiesWithoutCoordinates > 0 && (
                    <p className="border-b border-amber-500/20 bg-amber-500/10 px-4 py-3 text-xs text-amber-300">
                      {activitiesWithoutCoordinates} itinerary {activitiesWithoutCoordinates === 1 ? "activity has" : "activities have"} no coordinates, so {activitiesWithoutCoordinates === 1 ? "its" : "their"} distance and map marker are unavailable.
                    </p>
                  )}
                  <LeafletMap
                    center={mapCenter}
                    zoom={markers.length > 1 ? 6 : 12}
                    height="420px"
                    markers={markers}
                    selectedMarker={selectedMarker}
                    onMarkerClick={(marker) => setSelectedExperienceId(marker.id)}
                    showSearch
                    showFullscreen
                    showLegend={false}
                    currentLocation={currentPosition}
                    recenterSequence={currentLocation.state.requestSequence + recenterBump}
                    routeLines={routeLines}
                  />
                </section>

                <section>
                  <div className="mb-4 flex flex-col justify-between gap-2 sm:flex-row sm:items-end">
                    <div>
                      <h2 className="text-2xl font-bold">Your day-by-day draft</h2>
                      <p className="text-sm text-muted-foreground">
                        Reorder, replace, remove or add activities before you travel.
                      </p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={() => void saveItinerary()}
                        disabled={Boolean(savedItineraryId)}
                        className="btn-accent rounded-lg px-3 py-2 text-xs disabled:opacity-60"
                      >
                        {savedItineraryId ? "Saved to My Trips" : "Save itinerary"}
                      </button>
                      <button
                        type="button"
                        onClick={() => void buildPlan()}
                        className="flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-xs hover:bg-secondary"
                      >
                        <RefreshCw className="h-3.5 w-3.5" /> Regenerate
                      </button>
                    </div>
                  </div>

                  <div className="space-y-5">
                    {itineraryByDay.map(({ date, items }, dayIndex) => (
                      <article key={date} className="overflow-hidden rounded-2xl border border-border bg-card">
                        <header className="flex items-center justify-between gap-3 border-b border-border bg-secondary/30 px-4 py-3">
                          <div>
                            <p className="text-[10px] font-semibold uppercase tracking-widest text-accent">Day {dayIndex + 1}</p>
                            <h3 className="font-bold">{formatDay(date)}</h3>
                          </div>
                          <span className="rounded-full bg-background px-3 py-1 text-xs text-muted-foreground">
                            {items.length} {items.length === 1 ? "activity" : "activities"}
                          </span>
                        </header>

                        {backendPlan?.days[dayIndex] && (
                          <div className="border-b border-border px-4 py-3 text-xs text-muted-foreground">
                            <p>{backendPlan.days[dayIndex].explanation.summary}</p>
                            <p className="mt-1">
                              {backendPlan.days[dayIndex].totalDistanceKm.toFixed(1)} km · {backendPlan.days[dayIndex].totalTravelMinutes} min travel · {backendPlan.days[dayIndex].estimatedCost == null ? "No known cost" : formatUsdPrice(backendPlan.days[dayIndex].estimatedCost)}
                            </p>
                          </div>
                        )}

                        <div className="divide-y divide-border">
                          {items.map((item, index) => (
                            <ItineraryActivity
                              key={item.experience.id}
                              item={item}
                              selected={selectedExperienceId === item.experience.id}
                              canMoveUp={index > 0}
                              canMoveDown={index < items.length - 1}
                              onSelect={() => setSelectedExperienceId(item.experience.id)}
                              onRemove={() => removeActivity(item.experience.id)}
                              onReplace={() => replaceActivity(item.experience.id)}
                              onMove={(direction) => setItinerary((current) =>
                                moveItineraryItem(current, item.experience.id, direction))}
                              onDetails={() => void openExperienceDetails(item.experience.id)}
                              currentPosition={planningStartPosition}
                            />
                          ))}
                          {items.length === 0 && (
                            <p className="p-5 text-sm text-muted-foreground">This day has no activities yet.</p>
                          )}
                        </div>

                        {(backendPlan?.days[dayIndex]?.legs.length ?? 0) > 0 && (
                          <div className="border-t border-border bg-background/40 p-4">
                            <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Route legs</h4>
                            <div className="mt-2 space-y-2">
                              {backendPlan?.days[dayIndex].legs.map((leg, legIndex) => (
                                <div key={`${leg.fromOrder}-${leg.toOrder}-${legIndex}`} className="rounded-lg border border-border p-3 text-xs">
                                  <p className="font-semibold">
                                    Stop {leg.fromOrder} → {leg.toOrder} · {leg.mode}
                                    {leg.mode !== backendPlan.travelMode ? ` (fallback from ${backendPlan.travelMode})` : ""}
                                  </p>
                                  <p className="mt-1 text-muted-foreground">{leg.provider} · {leg.distanceKm.toFixed(1)} km · {leg.durationMinutes} min</p>
                                  {leg.steps.length > 0 && <p className="mt-2 text-muted-foreground">{leg.steps.join(" → ")}</p>}
                                  {leg.warnings.map((warning) => <p key={warning} className="mt-2 text-amber-300">{warning}</p>)}
                                </div>
                              ))}
                            </div>
                          </div>
                        )}

                        <div className="grid gap-2 border-t border-border bg-secondary/20 p-3 sm:grid-cols-[1fr_auto]">
                          <select
                            aria-label={`Add activity to ${formatDay(date)}`}
                            value={addSelections[date] ?? ""}
                            onChange={(event) => setAddSelections((current) => ({
                              ...current,
                              [date]: event.target.value,
                            }))}
                            className="min-w-0 rounded-lg border border-border bg-background px-3 py-2 text-xs"
                          >
                            <option value="">Choose another backend experience…</option>
                            {sortedUnusedExperiences.slice(0, 50).map((experience) => (
                              <option key={experience.id} value={experience.id}>
                                {experience.name} · {experience.category} · {formatExperiencePrice(experience.startingPricePerPerson, experience.priceRange)}
                                {planningStartPosition
                                  ? ` · ${formatDistance(calculateDistanceKm(planningStartPosition, experience))}`
                                  : ""}
                              </option>
                            ))}
                          </select>
                          <button
                            type="button"
                            disabled={!addSelections[date]}
                            onClick={() => addActivity(date)}
                            className="flex items-center justify-center gap-2 rounded-lg border border-border px-4 py-2 text-xs font-semibold disabled:opacity-40"
                          >
                            <Plus className="h-3.5 w-3.5" /> Add activity
                          </button>
                        </div>
                      </article>
                    ))}
                  </div>
                </section>
              </>
            )}
          </div>
        </div>
      </main>
      <ExperienceRecommendationAssistant
        region={region}
        onViewDetails={openExperienceDetails}
        onAddToItinerary={addRecommendedExperience}
      />
      {details && (
        <ExperienceDetailsModal
          experience={details.experience}
          availability={details.availability}
          initialReviews={details.reviews}
          initialInsight={details.insight}
          fallbackImage={cairoImage}
          onClose={() => setDetails(undefined)}
        />
      )}
      <Footer />
    </div>
  );
};

const EmptyFeature = ({
  icon: Icon,
  label,
}: {
  icon: typeof Compass;
  label: string;
}) => (
  <div className="rounded-xl border border-border bg-background/60 p-3">
    <Icon className="h-4 w-4 text-accent" />
    <p className="mt-2 text-xs font-medium">{label}</p>
  </div>
);

const SummaryCard = ({
  label,
  value,
  icon: Icon,
}: {
  label: string;
  value: string;
  icon: typeof Compass;
}) => (
  <article className="rounded-xl border border-border bg-card p-4">
    <div className="flex items-center gap-2 text-xs text-muted-foreground">
      <Icon className="h-4 w-4 text-accent" /> {label}
    </div>
    <p className="mt-2 text-lg font-bold">{value}</p>
  </article>
);

const ItineraryActivity = ({
  item,
  selected,
  canMoveUp,
  canMoveDown,
  onSelect,
  onRemove,
  onReplace,
  onMove,
  onDetails,
  currentPosition,
}: {
  item: ItineraryItem;
  selected: boolean;
  canMoveUp: boolean;
  canMoveDown: boolean;
  onSelect: () => void;
  onRemove: () => void;
  onReplace: () => void;
  onMove: (direction: -1 | 1) => void;
  onDetails: () => void;
  currentPosition?: {
    latitude: number;
    longitude: number;
  };
}) => {
  const experience = item.experience;
  const distance = currentPosition
    ? calculateDistanceKm(currentPosition, experience)
    : undefined;
  return (
    <div
      className={`grid cursor-pointer gap-4 p-4 transition sm:grid-cols-[5rem_8rem_minmax(0,1fr)_auto] sm:items-center ${
        selected ? "bg-accent/10 ring-1 ring-inset ring-accent/40" : "hover:bg-secondary/20"
      }`}
      onClick={onSelect}
    >
      <div className="flex items-center gap-2 text-sm font-semibold text-accent">
        <GripVertical className="h-4 w-4 text-muted-foreground" /> {item.time}
      </div>
      <img
        src={experience.featuredImages[0]?.link || cairoImage}
        alt={experience.name}
        className="h-24 w-full rounded-xl object-cover sm:h-20"
        onError={(event) => {
          event.currentTarget.src = cairoImage;
        }}
      />
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <h4 className="truncate font-bold">{experience.name}</h4>
          <span className="rounded-full bg-secondary px-2 py-0.5 text-[10px]">{experience.category}</span>
        </div>
        <p className="mt-1 line-clamp-1 text-xs text-muted-foreground">
          {experience.address || "Location details unavailable"}
        </p>
        <div className="mt-2 flex flex-wrap items-center gap-3 text-xs">
          <span className="flex items-center gap-1 text-gold">
            <Star className="h-3.5 w-3.5 fill-current" /> {experience.rating?.toFixed(1) ?? "Not rated"}
          </span>
          <span className="font-semibold text-accent">
            {formatExperiencePrice(experience.startingPricePerPerson, experience.priceRange)}
          </span>
          {experience.currentInsight?.popularityPercentage != null && (
            <span className="flex items-center gap-1 text-muted-foreground">
              <Users className="h-3.5 w-3.5" /> {experience.currentInsight.popularityPercentage}% busy
            </span>
          )}
          {currentPosition && (
            <span className={`flex items-center gap-1 ${
              distance == null ? "text-amber-400" : "text-sky-300"
            }`}>
              <Navigation className="h-3.5 w-3.5" /> {formatDistance(distance)}
            </span>
          )}
        </div>
      </div>
      <div className="flex flex-wrap gap-1 sm:justify-end" onClick={(event) => event.stopPropagation()}>
        <button
          type="button"
          onClick={onDetails}
          className="rounded-lg border border-border px-2 py-1 text-xs"
          aria-label={`View ${experience.name} details`}
        >
          Details
        </button>
        <button
          type="button"
          disabled={!canMoveUp}
          onClick={() => onMove(-1)}
          className="rounded-lg border border-border p-2 disabled:opacity-30"
          aria-label={`Move ${experience.name} earlier`}
        >
          <ArrowUp className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          disabled={!canMoveDown}
          onClick={() => onMove(1)}
          className="rounded-lg border border-border p-2 disabled:opacity-30"
          aria-label={`Move ${experience.name} later`}
        >
          <ArrowDown className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={onReplace}
          className="rounded-lg border border-border p-2"
          aria-label={`Replace ${experience.name}`}
        >
          <RefreshCw className="h-3.5 w-3.5" />
        </button>
        <button
          type="button"
          onClick={onRemove}
          className="rounded-lg border border-destructive/30 p-2 text-destructive"
          aria-label={`Remove ${experience.name}`}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  );
};

export default WhereToGo;
