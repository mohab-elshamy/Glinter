import { request } from "@/shared/lib/api-client";
import type {
  ExperienceResponseDto,
  ExperienceSummaryDto,
  ExperienceBookingResponseDto,
  CreateExperienceRequest,
  UpdateExperienceRequest,
  CreateExperienceBookingRequest,
  GetExperiencesRequest,
  PagedResponse,
  ExperienceCategory,
  ExperienceAvailabilityDto,
  CreateExperienceAvailabilityRequest,
  ExperienceBookingStatus,
  ExperienceReviewDto,
  CreateExperienceReviewRequest,
  ExperienceImageUploadDto,
  ExperienceMapItemDto,
  ExperienceReviewsForLlmDto,
  ExperienceVisitInsightDto,
  ImportExperiencesResult,
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
    return request<PagedResponse<ExperienceSummaryDto>>(`/experiences${query}`);
  },

  getExperienceById: (id: number) =>
    request<ExperienceResponseDto>(`/experiences/${id}`),

  getMapExperiences: (params?: GetExperiencesRequest) => {
    const query = params
      ? "?" + new URLSearchParams(
          Object.entries(params).reduce<Record<string, string>>((acc, [key, value]) => {
            if (value !== undefined && value !== null) acc[key] = String(value);
            return acc;
          }, {}),
        ).toString()
      : "";
    return request<ExperienceMapItemDto[]>(`/experiences/map${query}`);
  },

  getMyExperiences: () =>
    request<ExperienceResponseDto[]>("/experiences/mine"),

  createExperience: (data: CreateExperienceRequest) =>
    request<ExperienceResponseDto>("/experiences", { method: "POST", body: data }),

  updateExperience: (id: number, data: UpdateExperienceRequest) =>
    request<ExperienceResponseDto>(`/experiences/${id}`, { method: "PUT", body: data }),

  activateExperience: (id: number) =>
    request<ExperienceResponseDto>(`/experiences/${id}/activate`, { method: "PATCH" }),

  deactivateExperience: (id: number) =>
    request<ExperienceResponseDto>(`/experiences/${id}/deactivate`, { method: "PATCH" }),

  getCategories: () =>
    request<ExperienceCategory[]>("/experiences/categories"),

  getAvailability: (experienceId: number) =>
    request<ExperienceAvailabilityDto[]>(`/experiences/${experienceId}/availability`),

  getManagedAvailability: (experienceId: number) =>
    request<ExperienceAvailabilityDto[]>(`/experiences/${experienceId}/availability/manage`),

  createAvailability: (experienceId: number, data: CreateExperienceAvailabilityRequest) =>
    request<ExperienceAvailabilityDto>(`/experiences/${experienceId}/availability`, {
      method: "POST",
      body: data,
    }),

  updateAvailability: (
    availabilityId: string,
    data: CreateExperienceAvailabilityRequest & { isActive: boolean },
  ) => request<ExperienceAvailabilityDto>(`/experience-availability/${availabilityId}`, {
    method: "PUT",
    body: data,
  }),

  activateAvailability: (availabilityId: string) =>
    request<ExperienceAvailabilityDto>(`/experience-availability/${availabilityId}/activate`, { method: "PATCH" }),

  deactivateAvailability: (availabilityId: string) =>
    request<ExperienceAvailabilityDto>(`/experience-availability/${availabilityId}/deactivate`, { method: "PATCH" }),

  createBooking: (experienceId: number, data: CreateExperienceBookingRequest) =>
    request<ExperienceBookingResponseDto>(`/experiences/${experienceId}/bookings`, { method: "POST", body: data }),

  getMyBookings: () =>
    request<ExperienceBookingResponseDto[]>("/experience-bookings/my"),

  cancelBooking: (bookingId: string) =>
    request<ExperienceBookingResponseDto>(`/experience-bookings/${bookingId}/cancel`, { method: "PATCH" }),

  getBookings: (experienceId: number) =>
    request<ExperienceBookingResponseDto[]>(`/experiences/${experienceId}/bookings`),

  updateBookingStatus: (bookingId: string, status: ExperienceBookingStatus) =>
    request<ExperienceBookingResponseDto>(`/experience-bookings/${bookingId}/status`, {
      method: "PATCH",
      body: { status },
    }),

  getReviews: (experienceId: number, page = 1, pageSize = 20) =>
    request<ExperienceReviewDto[]>(`/experiences/${experienceId}/reviews?page=${page}&pageSize=${pageSize}`),

  createReview: (experienceId: number, data: CreateExperienceReviewRequest) =>
    request<ExperienceReviewDto>(`/experiences/${experienceId}/reviews`, { method: "POST", body: data }),

  getVisitInsights: (experienceId: number, visitAt?: string) => {
    const query = visitAt
      ? `?visitAt=${encodeURIComponent(visitAt)}`
      : "";
    return request<ExperienceVisitInsightDto>(`/experiences/${experienceId}/visit-insights${query}`);
  },

  uploadImage: (file: File) => {
    const body = new FormData();
    body.append("file", file);
    return request<ExperienceImageUploadDto>("/experiences/images", {
      method: "POST",
      body,
    });
  },

  importExperiences: (category: ExperienceCategory, file: File) => {
    const body = new FormData();
    body.append("category", category);
    body.append("file", file);
    return request<ImportExperiencesResult>("/experiences/import", {
      method: "POST",
      body,
    });
  },

  getReviewsForLlm: (experienceId: number) =>
    request<ExperienceReviewsForLlmDto>(`/experiences/${experienceId}/reviews/llm-input`),
};
