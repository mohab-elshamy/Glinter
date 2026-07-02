import type { HotelRecommendationRequest } from "@/shared/types/api";
import type { RegionHierarchyGids } from "@/shared/types/regions";
import type { BudgetLevel } from "@/shared/lib/budget-levels";

export const NATURAL_LANGUAGE_MAX_CHARACTERS = 1000;

export const buildStructuredRecommendationRequest = ({
  budgetLevel,
  selectedCategories,
  categoryWeights,
  selectedAmenities,
  limit,
  language,
  region,
}: {
  budgetLevel?: BudgetLevel;
  selectedCategories: string[];
  categoryWeights: Record<string, number | undefined>;
  selectedAmenities: string[];
  limit: number;
  language: "en" | "ar";
  region: RegionHierarchyGids;
}): HotelRecommendationRequest => ({
  budgetLevel,
  experienceCategories: selectedCategories.map((category) => ({
    category,
    weight: categoryWeights[category] || null,
  })),
  requestedAmenities: selectedAmenities,
  adm0Gid: region.adm0Gid,
  adm1Gid: region.adm1Gid,
  adm2Gid: region.adm2Gid,
  adm3Gid: region.adm3Gid,
  limit,
  preferredLanguage: language,
});
