import { request } from "@/shared/lib/api-client";
import type {
  ItineraryPlanRequest,
  ItineraryPlanResponse,
  NaturalLanguageItineraryPlanResponse,
  SaveItineraryRequest,
  SavedItinerary,
  WeatherForecast,
} from "@/shared/types/itineraries";

export const itinerariesApi = {
  plan: (data: ItineraryPlanRequest) =>
    request<ItineraryPlanResponse>("/itineraries/plan", {
      method: "POST",
      body: data,
    }),
  planNaturalLanguage: (
    data: { text: string } & Omit<ItineraryPlanRequest, "categories">,
  ) =>
    request<NaturalLanguageItineraryPlanResponse>(
      "/itineraries/plan/natural-language",
      { method: "POST", body: data },
    ),
  save: (data: SaveItineraryRequest) =>
    request<SavedItinerary>("/itineraries", { method: "POST", body: data }),
  list: () => request<SavedItinerary[]>("/itineraries"),
  get: (id: string) => request<SavedItinerary>(`/itineraries/${id}`),
  update: (
    id: string,
    data: Pick<SavedItinerary, "title" | "destination" | "estimatedTotalCost" | "currency"> & {
      expectedUpdatedAtUtc: string;
    },
  ) => request<SavedItinerary>(`/itineraries/${id}`, { method: "PUT", body: data }),
  replaceItems: (
    id: string,
    data: { expectedUpdatedAtUtc: string; items: SaveItineraryRequest["items"] },
  ) => request<SavedItinerary>(`/itineraries/${id}/items`, { method: "PUT", body: data }),
  remove: (id: string) =>
    request<void>(`/itineraries/${id}`, { method: "DELETE" }),
  weather: (params: {
    latitude: number;
    longitude: number;
    location?: string;
    startDate: string;
    endDate: string;
  }) => request<WeatherForecast>(
    `/weather/forecast?${new URLSearchParams(
      Object.entries(params).reduce<Record<string, string>>((query, [key, value]) => {
        if (value !== undefined) query[key] = String(value);
        return query;
      }, {}),
    )}`,
  ),
};
