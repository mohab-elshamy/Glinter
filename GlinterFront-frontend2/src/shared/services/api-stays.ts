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
};
