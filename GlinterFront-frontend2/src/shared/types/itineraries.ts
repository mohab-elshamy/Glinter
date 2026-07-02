import type { ExperienceCategory } from "./api";

export interface ItineraryPointRequest {
  latitude: number;
  longitude: number;
  label?: string;
}

export interface ItineraryPlanRequest {
  start: ItineraryPointRequest;
  end?: ItineraryPointRequest;
  date: string;
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
  recommendationScore?: number;
  primaryImage?: string;
  notes: string[];
}

export interface ItineraryPlanResponse {
  date: string;
  totalCandidateExperiences: number;
  selectedStopsCount: number;
  totalDurationMinutes: number;
  totalTravelMinutes: number;
  totalDistanceKm: number;
  score: number;
  warnings: string[];
  stops: ItineraryStop[];
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
    advice?: string;
  }>;
}
