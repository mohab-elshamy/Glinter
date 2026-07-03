import type { ExperienceCategory } from "@/shared/types/api";

const mappings: Record<string, ExperienceCategory> = {
  history: "Historical",
  museums: "Historical",
  museum: "Historical",
  culture: "Historical",
  archaeology: "Historical",
  historical: "Historical",
  تاريخ: "Historical",
  تاريخي: "Historical",
  متاحف: "Historical",
  ثقافة: "Historical",
  اثار: "Historical",
  food: "Dining",
  restaurants: "Dining",
  restaurant: "Dining",
  cuisine: "Dining",
  dining: "Dining",
  طعام: "Dining",
  مطاعم: "Dining",
  اكل: "Dining",
  nature: "Nature",
  beaches: "Nature",
  beach: "Nature",
  adventure: "Nature",
  parks: "Nature",
  park: "Nature",
  طبيعة: "Nature",
  شواطئ: "Nature",
  مغامرات: "Nature",
  حدائق: "Nature",
  shopping: "Shopping",
  markets: "Shopping",
  market: "Shopping",
  تسوق: "Shopping",
  اسواق: "Shopping",
  nightlife: "Nightlife",
  سهر: "Nightlife",
  حياةليلية: "Nightlife",
};

const normalize = (value: string) => value
  .trim()
  .toLocaleLowerCase()
  .replaceAll(/[^\p{L}\p{N}]+/gu, "");

export const mapInterestToCategory = (interest: string) =>
  mappings[normalize(interest)];

export const mapInterestsToCategories = (interests: string[]) =>
  [...new Set(interests.flatMap((interest) => {
    const category = mapInterestToCategory(interest);
    return category ? [category] : [];
  }))];
