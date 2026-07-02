import type { ExperienceCategory } from "./api";

export interface ItineraryPointRequest {
  latitude: number;
  longitude: number;
  label?: string;
}

export interface ItineraryPlanRequest {
  start: ItineraryPointRequest;
  origin?: ItineraryPointRequest;
  end?: ItineraryPointRequest;
  date: string;
  startDate?: string;
  endDate?: string;
  destination?: string;
  dayStartLocal?: string;
  dayEndLocal?: string;
  travelMode?: "Walking" | "Driving" | "Cycling" | "PublicTransit";
  fallbackTravelMode?: "Walking" | "Driving" | "Cycling" | "PublicTransit";
  pace?: "Relaxed" | "Balanced" | "Packed";
  categories: Array<{ category: ExperienceCategory; weight?: number }>;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  maxStops?: number;
  candidateLimit?: number;
  guestsCount?: number;
  includeMealBreaks?: boolean;
  returnToStart?: boolean;
  avoidLongWalking?: boolean;
  preferredLanguage?: "en" | "ar";
  budgetLevel?: number;
  totalBudget?: number;
  currency?: string;
}

export interface ItineraryStop {
  order: number;
  type: string;
  experienceId?: number;
  name: string;
  category?: ExperienceCategory;
  regionDisplayName?: string;
  address?: string;
  latitude: number;
  longitude: number;
  arrivalLocal: string;
  departureLocal: string;
  durationMinutes: number;
  estimatedCost?: number;
  explanation?: string;
  category?: string;
  rating?: number;
  imageUrl?: string;
  travelModeFromPrevious?: string;
  routeProviderFromPrevious?: string;
  routeGeometryFromPrevious?: { type: "LineString"; coordinates: number[][] };
  routeInstructionsFromPrevious?: string[];
  routeWarningsFromPrevious?: string[];
  distanceKmFromPrevious?: number;
  travelDurationMinutesFromPrevious?: number;
  recommendationScore?: number;
  primaryImage?: string;
  notes: string[];
}

export interface ItineraryLeg {
  fromOrder: number;
  toOrder: number;
  mode: "Walking" | "Driving" | "Cycling" | "PublicTransit";
  provider: string;
  distanceKm: number;
  durationMinutes: number;
  geometry?: {
    type: "LineString";
    coordinates: number[][];
  };
  steps: string[];
  warnings: string[];
}

export interface ItineraryDay {
  date: string;
  dayNumber: number;
  selectedStopsCount: number;
  totalCandidateExperiences: number;
  totalDurationMinutes: number;
  totalTravelMinutes: number;
  totalDistanceKm: number;
  estimatedCost?: number;
  unknownPriceStops: number;
  score: number;
  warnings: string[];
  stops: ItineraryStop[];
  legs: ItineraryLeg[];
  explanation: { summary: string; reasons: string[]; isAiGenerated: boolean };
}

export interface ItineraryPlanResponse {
  date: string;
  startDate: string;
  endDate: string;
  destination?: string;
  origin: ItineraryPointRequest;
  originSource: string;
  travelMode: "Walking" | "Driving" | "Cycling" | "PublicTransit";
  fallbackTravelMode: "Walking" | "Driving" | "Cycling" | "PublicTransit";
  pace: "Relaxed" | "Balanced" | "Packed";
  totalCandidateExperiences: number;
  selectedStopsCount: number;
  totalDurationMinutes: number;
  totalTravelMinutes: number;
  totalDistanceKm: number;
  score: number;
  estimatedTotalCost?: number;
  unknownPriceStops: number;
  totalBudget?: number;
  currency: string;
  budgetApplied: boolean;
  isWithinBudget?: boolean;
  warnings: string[];
  stops: ItineraryStop[];
  legs: ItineraryLeg[];
  explanation: { summary: string; reasons: string[]; isAiGenerated: boolean };
  days: ItineraryDay[];
}

export interface NaturalLanguageItineraryPlanResponse {
  inputText: string;
  interpretedRequest: ItineraryPlanRequest;
  classification: {
    isAiGenerated: boolean;
    confidence?: number;
    notes?: string;
  };
  itinerary: ItineraryPlanResponse;
}

export interface SavedItineraryItem {
  id: string;
  dayNumber: number;
  sortOrder: number;
  entityType: string;
  entityId?: number;
  name: string;
  latitude: number;
  longitude: number;
  startTime?: string;
  endTime?: string;
  estimatedDurationMinutes?: number;
  estimatedCost?: number;
  explanation?: string;
}

export interface SavedItinerary {
  id: string;
  title: string;
  destination?: string;
  startDate: string;
  endDate: string;
  preferredLanguage: string;
  estimatedTotalCost?: number;
  currency?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  plannerExplanation?: string;
  warnings: string[];
  recommendationScore?: number;
  totalDistanceKm?: number;
  totalTravelMinutes?: number;
  pace?: string;
  travelMode?: string;
  fallbackTravelMode?: string;
  origin?: ItineraryPointRequest;
  weatherLatitude?: number;
  weatherLongitude?: number;
  weatherLocation?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  items: SavedItineraryItem[];
}

export interface SaveItineraryRequest {
  title: string;
  destination?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  startDate: string;
  endDate: string;
  preferredLanguage: string;
  estimatedTotalCost?: number;
  currency?: string;
  plannerExplanation?: string;
  warnings?: string[];
  recommendationScore?: number;
  totalDistanceKm?: number;
  totalTravelMinutes?: number;
  pace?: string;
  travelMode?: string;
  fallbackTravelMode?: string;
  origin?: ItineraryPointRequest;
  weatherLatitude?: number;
  weatherLongitude?: number;
  weatherLocation?: string;
  items: Array<Omit<SavedItineraryItem, "id"> & { id?: string }>;
}

export interface WeatherForecast {
  location?: string;
  isAvailable: boolean;
  unavailableReason?: string;
  providerDataTimestampUtc?: string;
  days: Array<{
    date: string;
    temperatureMinC?: number;
    temperatureMaxC?: number;
    condition?: string;
    precipitationProbabilityPercent?: number;
    windSpeedKph?: number;
    humidityPercent?: number;
    advice?: string;
  }>;
}
