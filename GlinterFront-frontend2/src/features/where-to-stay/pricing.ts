import { parseISO, differenceInDays, getMonth } from "date-fns";

export function getSeasonalMultiplier(month: number): number {
  if (month >= 11 || month <= 1) return 1.4;
  if (month >= 5 && month <= 7) return 0.8;
  if (month >= 2 && month <= 4) return 1.1;
  return 1.0;
}

export function getLengthDiscount(nights: number): number {
  if (nights >= 14) return 0.2;
  if (nights >= 7) return 0.1;
  return 0;
}

export function getNights(checkin: string, checkout: string): number {
  if (!checkin || !checkout) return 0;
  const start = parseISO(checkin);
  const end = parseISO(checkout);
  const nights = differenceInDays(end, start);
  return Math.max(0, nights);
}

export interface PriceBreakdown {
  basePrice: number;
  adjustedNightly: number;
  seasonalMultiplier: number;
  nights: number;
  subtotal: number;
  discountPercent: number;
  discountAmount: number;
  total: number;
}

export function computePrice(basePrice: number, checkin: string, checkout: string): PriceBreakdown {
  const nights = getNights(checkin, checkout);
  if (nights === 0) {
    return {
      basePrice,
      adjustedNightly: basePrice,
      seasonalMultiplier: 1,
      nights: 0,
      subtotal: 0,
      discountPercent: 0,
      discountAmount: 0,
      total: 0,
    };
  }

  const month = checkin ? getMonth(parseISO(checkin)) : 0;
  const seasonalMultiplier = getSeasonalMultiplier(month);
  const adjustedNightly = Math.round(basePrice * seasonalMultiplier);
  const subtotal = adjustedNightly * nights;
  const discountPercent = getLengthDiscount(nights);
  const discountAmount = Math.round(subtotal * discountPercent);
  const total = subtotal - discountAmount;

  return {
    basePrice,
    adjustedNightly,
    seasonalMultiplier,
    nights,
    subtotal,
    discountPercent,
    discountAmount,
    total,
  };
}
