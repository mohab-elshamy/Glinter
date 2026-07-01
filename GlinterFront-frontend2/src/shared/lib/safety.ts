import type {
  SafetyDataState,
  SafetyPeriod,
  SafetyScore,
} from "@/shared/types/safety";

export const WEEKLY_SAFETY_STALE_AFTER_HOURS = 8 * 24;

export const getSafetyDataState = (
  value: SafetyScore,
  period: SafetyPeriod,
  now = new Date(),
): SafetyDataState => {
  if (value.score == null) return "unavailable";
  if (period === "historical") return "available";
  if (!value.calculatedAtUtc) return "stale";

  const calculatedAt = new Date(value.calculatedAtUtc);
  if (Number.isNaN(calculatedAt.getTime())) return "stale";

  const ageHours = (now.getTime() - calculatedAt.getTime()) / 3_600_000;
  return ageHours > WEEKLY_SAFETY_STALE_AFTER_HOURS ? "stale" : "available";
};

export const getScoreForPeriod = (
  area: { weeklyScore: SafetyScore; historicalScore: SafetyScore },
  period: SafetyPeriod,
) => period === "weekly" ? area.weeklyScore : area.historicalScore;

export const getSafetyColor = (score?: number | null) => {
  if (score == null) return "#64748b";
  if (score >= 80) return "#22c55e";
  if (score >= 60) return "#84cc16";
  if (score >= 40) return "#f59e0b";
  return "#ef4444";
};
