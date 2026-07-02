import { request } from "@/shared/lib/api-client";
import type {
  StayResponseDto,
  CreateStayRequest,
  UpdateStayRequest,
  GetStaysRequest,
  StayBookingResponseDto,
  CreateStayBookingRequest,
  PagedResponse,
  StayReviewDto,
  CreateStayReviewRequest,
  StayBookingStatus,
  StayRegionStatsRequest,
  StayRegionStatsDto,
  StayImageUploadDto,
  ImportStaysResult,
  StayReviewsForLlmDto,
  HotelRecommendationRequest,
  HotelRecommendationResponse,
  NaturalLanguageHotelRecommendationRequest,
  NaturalLanguageHotelRecommendationResponse,
} from "@/shared/types/api";

export const staysApi = {
  getStays: (params?: GetStaysRequest) => {
    const query = params
      ? "?" + new URLSearchParams(
          Object.entries(params).reduce<Record<string, string>>((acc, [k, v]) => {
            if (v !== undefined && v !== null) acc[k] = String(v);
            return acc;
          }, {})
        ).toString()
      : "";
    return request<PagedResponse<StayResponseDto>>(`/stays${query}`);
  },

  getStayById: (id: number) =>
    request<StayResponseDto>(`/stays/${id}`),

  getRegionStats: (params: StayRegionStatsRequest) => {
    const query = new URLSearchParams(
      Object.entries(params).reduce<Record<string, string>>((acc, [key, value]) => {
        if (value !== undefined && value !== null) acc[key] = String(value);
        return acc;
      }, {}),
    );
    return request<StayRegionStatsDto[]>(`/stays/region-stats?${query}`);
  },

  getMyStays: () =>
    request<StayResponseDto[]>("/stays/mine"),

  createStay: (data: CreateStayRequest) =>
    request<StayResponseDto>("/stays", { method: "POST", body: data }),

  updateStay: (id: number, data: UpdateStayRequest) =>
    request<StayResponseDto>(`/stays/${id}`, { method: "PUT", body: data }),

  deactivateStay: (id: number) =>
    request<StayResponseDto>(`/stays/${id}/deactivate`, { method: "PATCH" }),

  activateStay: (id: number) =>
    request<StayResponseDto>(`/stays/${id}/activate`, { method: "PATCH" }),

  getBookings: (stayId: number) =>
    request<StayBookingResponseDto[]>(`/stays/${stayId}/bookings`),

  createBooking: (stayId: number, data: CreateStayBookingRequest) =>
    request<StayBookingResponseDto>(`/stays/${stayId}/bookings`, { method: "POST", body: data }),

  cancelBooking: (bookingId: string) =>
    request<StayBookingResponseDto>(`/stay-bookings/${bookingId}/cancel`, { method: "PATCH" }),

  getMyBookings: () =>
    request<StayBookingResponseDto[]>("/stay-bookings/my"),

  updateBookingStatus: (bookingId: string, status: StayBookingStatus) =>
    request<StayBookingResponseDto>(`/stay-bookings/${bookingId}/status`, {
      method: "PATCH",
      body: { status },
    }),

  getReviews: (stayId: number, page = 1, pageSize = 20) =>
    request<StayReviewDto[]>(`/stays/${stayId}/reviews?page=${page}&pageSize=${pageSize}`),

  createReview: (stayId: number, data: CreateStayReviewRequest) =>
    request<StayReviewDto>(`/stays/${stayId}/reviews`, { method: "POST", body: data }),

  uploadImage: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<StayImageUploadDto>("/stays/images", {
      method: "POST",
      body: form,
    });
  },

  importStays: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<ImportStaysResult>("/stays/import", {
      method: "POST",
      body: form,
    });
  },

  getReviewsForLlm: (stayId: number) =>
    request<StayReviewsForLlmDto>(`/stays/${stayId}/reviews/llm-input`),

  getRecommendations: (data: HotelRecommendationRequest) =>
    request<HotelRecommendationResponse>("/stays/recommendations", {
      method: "POST",
      body: data,
    }),

  getNaturalLanguageRecommendations: (data: NaturalLanguageHotelRecommendationRequest) =>
    request<NaturalLanguageHotelRecommendationResponse>(
      "/stays/recommendations/natural-language",
      { method: "POST", body: data },
    ),

  getFavorites: (page = 1, pageSize = 20) =>
    request<PagedResponse<{
      id: number;
      name: string;
      price?: number;
      rating?: number;
      reviews?: number;
      primaryImage?: string;
      locationSummaryDescription?: string;
      favoritedAtUtc: string;
    }>>(`/stays/favorites?page=${page}&pageSize=${pageSize}`),

  getAllFavorites: async () => {
    const items: Awaited<ReturnType<typeof staysApi.getFavorites>>["items"] = [];
    let totalCount = 0;
    for (let page = 1; page <= 100; page += 1) {
      const result = await staysApi.getFavorites(page, 50);
      totalCount = result.totalCount;
      items.push(...result.items);
      if (items.length >= totalCount || result.items.length === 0) break;
    }
    return { items, totalCount };
  },

  getFavoriteStatuses: (stayIds: number[]) =>
    request<{ favoriteStayIds: number[] }>("/stays/favorite-statuses", {
      method: "POST",
      body: { stayIds },
    }),

  getFavoriteStatus: (stayId: number) =>
    request<{ stayId: number; isFavorite: boolean }>(
      `/stays/${stayId}/favorite-status`,
    ),

  addFavorite: (stayId: number) =>
    request<{ stayId: number; isFavorite: boolean }>(
      `/stays/${stayId}/favorite`,
      { method: "POST" },
    ),

  removeFavorite: (stayId: number) =>
    request<void>(`/stays/${stayId}/favorite`, { method: "DELETE" }),
};
