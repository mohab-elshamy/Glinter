import { parseISO, differenceInDays } from "date-fns";

export function getNights(checkin: string, checkout: string): number {
  if (!checkin || !checkout) return 0;
  const start = parseISO(checkin);
  const end = parseISO(checkout);
  const nights = differenceInDays(end, start);
  return Math.max(0, nights);
}

export interface PriceBreakdown {
  nightlyPrice: number | null;
  nights: number;
  total: number | null;
}

export function computePrice(
  nightlyPrice: number | null | undefined,
  checkin: string,
  checkout: string,
): PriceBreakdown {
  const nights = getNights(checkin, checkout);
  return {
    nightlyPrice: nightlyPrice ?? null,
    nights,
    total: nightlyPrice == null || nights === 0 ? null : nightlyPrice * nights,
  };
}
