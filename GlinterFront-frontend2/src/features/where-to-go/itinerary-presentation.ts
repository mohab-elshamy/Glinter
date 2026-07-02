import type { MapRouteLine } from "@/components/LeafletMap";
import type { ItineraryPlanResponse } from "@/shared/types/itineraries";

export const buildItineraryRouteLines = (
  plan?: ItineraryPlanResponse,
): MapRouteLine[] => (plan?.days ?? []).flatMap((day) =>
  day.legs.flatMap((leg, index) => {
    const positions = (leg.geometry?.coordinates ?? []).flatMap((coordinate) =>
      coordinate.length >= 2 &&
      Number.isFinite(coordinate[0]) &&
      Number.isFinite(coordinate[1])
        ? [[coordinate[1], coordinate[0]] as [number, number]]
        : []);
    return positions.length < 2 ? [] : [{
      id: `${day.dayNumber}-${leg.fromOrder}-${leg.toOrder}-${index}`,
      positions,
      dashed: leg.warnings.length > 0,
      label: `${leg.mode} · ${leg.distanceKm.toFixed(1)} km · ${leg.durationMinutes} min · ${leg.provider}`,
    }];
  }));

export const formatBudgetFit = (plan: ItineraryPlanResponse) => {
  if (!plan.budgetApplied) return "Budget not applied";
  if (plan.totalBudget == null) return "Budget preference applied";
  return plan.isWithinBudget ? "Within budget" : "Over budget";
};
