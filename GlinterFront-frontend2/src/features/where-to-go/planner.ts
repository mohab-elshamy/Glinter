import type { ExperienceCategory, ExperienceSummaryDto } from "@/shared/types/api";

export type ActivityLevel = "Relaxed" | "Balanced" | "Packed";

export interface ItineraryItem {
  experience: ExperienceSummaryDto;
  date: string;
  time: string;
}

export interface PlannerInput {
  startDate: string;
  endDate: string;
  budget?: number;
  interests: ExperienceCategory[];
  activityLevel: ActivityLevel;
  prioritizeNearest?: boolean;
  currentPosition?: {
    latitude: number;
    longitude: number;
  };
}

const timeSlots: Record<ActivityLevel, string[]> = {
  Relaxed: ["10:00", "15:00"],
  Balanced: ["09:30", "13:00", "17:00"],
  Packed: ["09:00", "12:00", "15:00", "18:00"],
};

const parseDate = (value: string) => new Date(`${value}T00:00:00.000Z`);

export const enumerateTripDates = (startDate: string, endDate: string, maximumDays = 14) => {
  if (!startDate || !endDate) return [];
  const start = parseDate(startDate);
  const end = parseDate(endDate);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end < start) return [];

  const dates: string[] = [];
  const cursor = new Date(start);
  while (cursor <= end && dates.length < maximumDays) {
    dates.push(cursor.toISOString().slice(0, 10));
    cursor.setUTCDate(cursor.getUTCDate() + 1);
  }
  return dates;
};

export const getTimeSlots = (activityLevel: ActivityLevel) => timeSlots[activityLevel];

const rankExperience = (experience: ExperienceSummaryDto) =>
  (experience.rating ?? 0) * 1_000
  + Math.min(experience.reviews ?? 0, 999)
  + (experience.currentInsight?.popularityPercentage ?? 0);

export const calculateDistanceKm = (
  from: { latitude: number; longitude: number },
  to: { latitude?: number; longitude?: number },
) => {
  if (to.latitude == null || to.longitude == null) return undefined;
  const toRadians = (degrees: number) => degrees * (Math.PI / 180);
  const earthRadiusKm = 6_371;
  const latitudeDelta = toRadians(to.latitude - from.latitude);
  const longitudeDelta = toRadians(to.longitude - from.longitude);
  const startLatitude = toRadians(from.latitude);
  const endLatitude = toRadians(to.latitude);
  const haversine = Math.sin(latitudeDelta / 2) ** 2
    + Math.cos(startLatitude) * Math.cos(endLatitude) * Math.sin(longitudeDelta / 2) ** 2;
  return earthRadiusKm * 2 * Math.atan2(Math.sqrt(haversine), Math.sqrt(1 - haversine));
};

export const buildFrontendItinerary = (
  experiences: ExperienceSummaryDto[],
  input: PlannerInput,
) => {
  const dates = enumerateTripDates(input.startDate, input.endDate);
  if (dates.length === 0) return [];

  const interested = input.interests.length > 0
    ? experiences.filter((experience) => input.interests.includes(experience.category))
    : experiences;
  const candidates = [...interested].sort((left, right) => {
    if (input.prioritizeNearest && input.currentPosition) {
      const leftDistance = calculateDistanceKm(input.currentPosition, left);
      const rightDistance = calculateDistanceKm(input.currentPosition, right);
      if (leftDistance == null && rightDistance != null) return 1;
      if (leftDistance != null && rightDistance == null) return -1;
      if (leftDistance != null && rightDistance != null && leftDistance !== rightDistance) {
        return leftDistance - rightDistance;
      }
    }
    return rankExperience(right) - rankExperience(left);
  });
  const slots = getTimeSlots(input.activityLevel);
  const budget = input.budget && input.budget > 0 ? input.budget : undefined;
  let knownCost = 0;
  let candidateIndex = 0;
  const itinerary: ItineraryItem[] = [];

  for (const date of dates) {
    for (const time of slots) {
      let selected: ExperienceSummaryDto | undefined;
      while (candidateIndex < candidates.length && !selected) {
        const candidate = candidates[candidateIndex++];
        const price = candidate.startingPricePerPerson;
        if (budget != null && price != null && knownCost + price > budget) continue;
        selected = candidate;
      }
      if (!selected) break;
      knownCost += selected.startingPricePerPerson ?? 0;
      itinerary.push({ experience: selected, date, time });
    }
  }

  return itinerary;
};

export const calculateKnownExperienceCost = (items: ItineraryItem[]) =>
  items.reduce((total, item) => total + (item.experience.startingPricePerPerson ?? 0), 0);

export const countUnknownPrices = (items: ItineraryItem[]) =>
  items.filter((item) => item.experience.startingPricePerPerson == null).length;

export const moveItineraryItem = (
  items: ItineraryItem[],
  experienceId: number,
  direction: -1 | 1,
) => {
  const index = items.findIndex((item) => item.experience.id === experienceId);
  if (index < 0) return items;
  const target = index + direction;
  if (target < 0 || target >= items.length || items[target].date !== items[index].date) return items;
  const next = [...items];
  const currentExperience = next[index].experience;
  next[index] = { ...next[index], experience: next[target].experience };
  next[target] = { ...next[target], experience: currentExperience };
  return next;
};
