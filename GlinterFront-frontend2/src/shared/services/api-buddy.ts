import { request } from "@/shared/lib/api-client";
import type {
  BuddyAvailabilityDto,
  BuddyAvailabilityRequest,
  BuddyBookingDto,
  BuddyReviewDto,
  BuddyReviewSummaryDto,
} from "@/shared/types/api";

export const buddyQueryKeys = {
  all: ["buddy"] as const,
  mine: ["buddy", "requests", "mine"] as const,
  incoming: ["buddy", "requests", "incoming"] as const,
  availability: (buddyUserId: string) =>
    ["buddy", "availability", buddyUserId] as const,
  managedAvailability: (buddyUserId: string) =>
    ["buddy", "availability", buddyUserId, "manage"] as const,
  reviews: (buddyUserId: string) =>
    ["buddy", "reviews", buddyUserId] as const,
  reviewSummary: (buddyUserId: string) =>
    ["buddy", "review-summary", buddyUserId] as const,
};

export const buddyApi = {
  getAvailability: (buddyUserId: string) =>
    request<BuddyAvailabilityDto[]>(
      `/local-buddies/${buddyUserId}/availability`,
    ),

  getManagedAvailability: (buddyUserId: string) =>
    request<BuddyAvailabilityDto[]>(
      `/local-buddies/${buddyUserId}/availability/manage`,
    ),

  createAvailability: (
    buddyUserId: string,
    data: BuddyAvailabilityRequest,
  ) => request<BuddyAvailabilityDto>(
    `/local-buddies/${buddyUserId}/availability`,
    { method: "POST", body: data },
  ),

  updateAvailability: (
    availabilityId: string,
    data: BuddyAvailabilityRequest,
  ) => request<BuddyAvailabilityDto>(
    `/buddy-availability/${availabilityId}`,
    { method: "PUT", body: data },
  ),

  setAvailabilityActive: (availabilityId: string, active: boolean) =>
    request<BuddyAvailabilityDto>(
      `/buddy-availability/${availabilityId}/${active ? "activate" : "deactivate"}`,
      { method: "PATCH" },
    ),

  createRequest: (
    localBuddyUserId: string,
    availabilityId: string,
    notes?: string,
  ) => request<BuddyBookingDto>("/buddy/requests", {
    method: "POST",
    body: { localBuddyUserId, availabilityId, notes },
  }),

  getMine: () => request<BuddyBookingDto[]>("/buddy/requests/mine"),

  getIncoming: () =>
    request<BuddyBookingDto[]>("/buddy/requests/incoming"),

  getById: (requestId: string) =>
    request<BuddyBookingDto>(`/buddy/requests/${requestId}`),

  accept: (requestId: string) =>
    request<BuddyBookingDto>(`/buddy/requests/${requestId}/accept`, {
      method: "PATCH",
    }),

  reject: (requestId: string) =>
    request<BuddyBookingDto>(`/buddy/requests/${requestId}/reject`, {
      method: "PATCH",
    }),

  cancel: (requestId: string) =>
    request<BuddyBookingDto>(`/buddy/requests/${requestId}/cancel`, {
      method: "PATCH",
    }),

  createReview: (
    requestId: string,
    data: { rating: number; reviewText: string },
  ) => request<BuddyReviewDto>(`/buddy/requests/${requestId}/review`, {
    method: "POST",
    body: data,
  }),

  getReviews: (buddyUserId: string) =>
    request<BuddyReviewDto[]>(`/local-buddies/${buddyUserId}/reviews`),

  getReviewSummary: (buddyUserId: string) =>
    request<BuddyReviewSummaryDto>(
      `/local-buddies/${buddyUserId}/review-summary`,
    ),
};
