import type { ExperienceCategory } from "./api";

export interface ExperienceRecommendationCategoryPreference {
  category: ExperienceCategory;
  weight?: number;
}

export interface ExperienceRecommendationRequest {
  categories: ExperienceRecommendationCategoryPreference[];
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  visitAtLocal?: string;
  guestsCount?: number;
  crowdPreference?: "Quiet" | "Balanced" | "Lively";
  bookableOnly?: boolean;
  forItinerary?: boolean;
  limit?: number;
  preferredLanguage?: "en" | "ar";
}

export interface NaturalLanguageExperienceRecommendationRequest {
  text: string;
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  visitAtLocal?: string;
  guestsCount?: number;
  forItinerary?: boolean;
  limit?: number;
  preferredLanguage?: "en" | "ar";
}

export interface ExperienceRecommendationItem {
  ranking: number;
  experienceId: number;
  name: string;
  category: ExperienceCategory;
  address?: string;
  description?: string;
  regionDisplayName?: string;
  latitude: number;
  longitude: number;
  distanceKm?: number;
  rating?: number;
  reviews?: number;
  priceRange?: string;
  startingPricePerPerson?: number;
  primaryImage?: string;
  finalScore: number;
  amenities: string[];
  explanation: {
    shortExplanation: string;
    reasons: string[];
    bestFor: string[];
    isAiGenerated: boolean;
  };
}

export interface ExperienceRecommendationResponse {
  totalCandidates: number;
  evaluatedCandidates: number;
  returnedCount: number;
  items: ExperienceRecommendationItem[];
}

export interface NaturalLanguageExperienceRecommendationResponse
  extends ExperienceRecommendationResponse {
  inputText: string;
  classificationNotes?: string;
}
