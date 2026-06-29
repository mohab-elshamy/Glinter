import { request } from "@/shared/lib/api-client";
import type {
  StaySummaryDto,
  StayResponseDto,
  CreateStayRequest,
  UpdateStayRequest,
  GetStaysRequest,
  StayBookingResponseDto,
  CreateStayBookingRequest,
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
    return request<StaySummaryDto[]>(`/stays${query}`);
  },

  getStayById: (id: string) =>
    request<StayResponseDto>(`/stays/${id}`),

  getStaysByArea: (areaId: string) =>
    request<StaySummaryDto[]>(`/stays/by-area/${areaId}`),

  createStay: (data: CreateStayRequest) =>
    request<StayResponseDto>("/stays", { method: "POST", body: data }),

  updateStay: (id: string, data: UpdateStayRequest) =>
    request<StayResponseDto>(`/stays/${id}`, { method: "PUT", body: data }),

  deactivateStay: (id: string) =>
    request<StayResponseDto>(`/stays/${id}/deactivate`, { method: "PATCH" }),

  activateStay: (id: string) =>
    request<StayResponseDto>(`/stays/${id}/activate`, { method: "PATCH" }),

  getBookings: (stayId: string) =>
    request<StayBookingResponseDto[]>(`/stays/${stayId}/bookings`),

  createBooking: (stayId: string, data: CreateStayBookingRequest) =>
    request<StayBookingResponseDto>(`/stays/${stayId}/bookings`, { method: "POST", body: data }),

  cancelBooking: (bookingId: string) =>
    request<StayBookingResponseDto>(`/stay-bookings/${bookingId}/cancel`, { method: "PATCH" }),
};
