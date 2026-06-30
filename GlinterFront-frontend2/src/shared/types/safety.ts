export interface SafetyScore {
  score?: number | null;
  generalSafetyDescription?: string | null;
  trendingEventDescription?: string | null;
  calculatedAtUtc?: string | null;
  newsItemCount: number;
}

export interface DistrictSafetyIndex {
  adm2Gid: number;
  adm1Gid: number;
  nameEn: string;
  nameAr?: string | null;
  weeklyScore: SafetyScore;
  historicalScore: SafetyScore;
}

export type SafetyPeriod = "weekly" | "historical";

export type SafetyDataState = "available" | "stale" | "unavailable";
