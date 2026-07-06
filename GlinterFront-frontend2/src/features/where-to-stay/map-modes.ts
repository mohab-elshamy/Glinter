import type { Hotel } from "./types";

export type StayMapMode = "standard" | "safety" | "comfort" | "price";

export function getComfortScore(hotel: Hotel): number {
  const ratingPoints = Math.min(70, Math.max(0, hotel.rating) * 14);
  const reviewPoints = Math.min(15, Math.log10(Math.max(0, hotel.reviews) + 1) * 6);
  const amenityPoints = Math.min(15, hotel.amenities.length * 3);
  return Math.round(ratingPoints + reviewPoints + amenityPoints);
}

export function getScoreColor(score?: number | null): string {
  if (score == null) return "#64748b";
  if (score >= 80) return "#22c55e";
  if (score >= 60) return "#84cc16";
  if (score >= 40) return "#f59e0b";
  return "#ef4444";
}

export function getPriceColor(price?: number, averagePrice?: number): string {
  if (price == null || averagePrice == null || averagePrice <= 0) return "#64748b";
  const ratio = price / averagePrice;
  if (ratio <= 0.75) return "#22c55e";
  if (ratio <= 1) return "#84cc16";
  if (ratio <= 1.25) return "#f59e0b";
  return "#ef4444";
}

export function getIndexColor(indexValue?: number | null): string {
  if (indexValue == null) return "#64748b";
  if (indexValue <= 75) return "#22c55e";
  if (indexValue <= 100) return "#84cc16";
  if (indexValue <= 125) return "#f59e0b";
  return "#ef4444";
}

export const getPriceIndexColor = getIndexColor;

export function getComfortIndexColor(indexValue?: number | null): string {
  if (indexValue == null) return "#64748b";
  if (indexValue >= 125) return "#22c55e";
  if (indexValue >= 100) return "#84cc16";
  if (indexValue >= 75) return "#f59e0b";
  return "#ef4444";
}
