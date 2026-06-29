import { request } from "@/shared/lib/api-client";
import type {
  ExperienceResponseDto,
  ExperienceSummaryDto,
  ExperienceBookingResponseDto,
  ExperienceCategoryResponseDto,
  VibeResponseDto,
  CreateExperienceBookingRequest,
  GetExperiencesRequest,
} from "@/shared/types/api";

export const experiencesApi = {
  getExperiences: (params?: GetExperiencesRequest) => {
    const query = params
      ? "?" + new URLSearchParams(
          Object.entries(params).reduce<Record<string, string>>((acc, [k, v]) => {
            if (v !== undefined && v !== null) acc[k] = String(v);
            return acc;
          }, {})
        ).toString()
      : "";
    return request<ExperienceSummaryDto[]>(`/experiences${query}`);
  },

  getExperienceById: (id: string) =>
    request<ExperienceResponseDto>(`/experiences/${id}`),

  getMyExperiences: () =>
    request<ExperienceResponseDto[]>("/experiences/my"),

  createExperience: (data: Record<string, unknown>) =>
    request<ExperienceResponseDto>("/experiences", { method: "POST", body: data }),

  getCategories: () =>
    request<ExperienceCategoryResponseDto[]>("/experience-categories"),

  getVibes: () =>
    request<VibeResponseDto[]>("/vibes"),

  createBooking: (experienceId: string, data: CreateExperienceBookingRequest) =>
    request<ExperienceBookingResponseDto>(`/experiences/${experienceId}/bookings`, { method: "POST", body: data }),

  getMyBookings: () =>
    request<ExperienceBookingResponseDto[]>("/experience-bookings/my"),

  cancelBooking: (bookingId: string) =>
    request<void>(`/experience-bookings/${bookingId}/cancel`, { method: "PATCH" }),
};
