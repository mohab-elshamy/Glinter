import { request } from "@/shared/lib/api-client";
import type {
  TravelerProfileResponse,
  LocalBuddyListItemResponse,
  LocalBuddyProfileResponse,
  TravelerProfileRequest,
  InterestResponse,
  PagedResponse,
  FollowStatusResponse,
  BuddyAvailabilityDto,
  BuddyAvailabilityRequest,
  BuddyBookingDto,
  BuddyBookingStatus,
  BuddyReviewDto,
} from "@/shared/types/api";

export interface BusinessProfileResponse {
  profileId: string;
  userId: string;
  profileType: "HotelOwner" | "ExperienceProvider";
  businessName: string;
  contactPersonName?: string | null;
  phoneNumber?: string | null;
  description?: string | null;
  profileImageUrl?: string | null;
  followersCount: number;
  followingCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface LocalBuddyProfileRequest {
  displayName: string;
  bio?: string;
  city: string;
  languages?: string;
  interestIds: string[];
}

export interface BusinessProfileRequest {
  businessName: string;
  contactPersonName?: string;
  phoneNumber?: string;
  description?: string;
}

export type MyProfileResponse =
  | TravelerProfileResponse
  | LocalBuddyProfileResponse
  | BusinessProfileResponse;

const getAllLocalBuddies = async () => {
  const items: LocalBuddyListItemResponse[] = [];
  for (let page = 1; page <= 100; page += 1) {
    const response = await request<PagedResponse<LocalBuddyListItemResponse>>(
      `/local-buddies?page=${page}&pageSize=50`,
    );
    items.push(...response.items);
    if (items.length >= response.totalCount || response.items.length === 0) break;
  }
  return items;
};

export const profilesApi = {
  getMyProfile: () =>
    request<MyProfileResponse>("/profiles/me"),

  getInterests: () =>
    request<InterestResponse[]>("/interests"),

  updateTravelerProfile: (data: TravelerProfileRequest) =>
    request<TravelerProfileResponse>("/profiles/traveler", {
      method: "PUT",
      body: data,
    }),

  updateLocalBuddyProfile: (data: LocalBuddyProfileRequest) =>
    request<LocalBuddyProfileResponse>("/profiles/local-buddy", {
      method: "PUT",
      body: data,
    }),

  updateHotelOwnerProfile: (data: BusinessProfileRequest) =>
    request<BusinessProfileResponse>("/profiles/hotel-owner", {
      method: "PUT",
      body: data,
    }),

  updateExperienceProviderProfile: (data: BusinessProfileRequest) =>
    request<BusinessProfileResponse>("/profiles/experience-provider", {
      method: "PUT",
      body: data,
    }),

  updateProfileImage: (profileImageUrl: string) =>
    request<MyProfileResponse>("/profiles/image", {
      method: "PATCH",
      body: { profileImageUrl },
    }),

  getLocalBuddies: getAllLocalBuddies,

  getLocalBuddy: (userId: string) =>
    request<LocalBuddyProfileResponse>(`/local-buddies/${userId}`),

  followUser: (userId: string) =>
    request<FollowStatusResponse>(`/profiles/users/${userId}/follow`, { method: "POST" }),

  unfollowUser: (userId: string) =>
    request<FollowStatusResponse>(`/profiles/users/${userId}/follow`, { method: "DELETE" }),

  getFollowStatus: (userId: string) =>
    request<FollowStatusResponse>(`/profiles/users/${userId}/follow-status`),

  getBuddyAvailability: (userId: string) =>
    request<BuddyAvailabilityDto[]>(`/local-buddies/${userId}/availability`),

  getManagedBuddyAvailability: (userId: string) =>
    request<BuddyAvailabilityDto[]>(`/local-buddies/${userId}/availability/manage`),

  createBuddyAvailability: (userId: string, data: BuddyAvailabilityRequest) =>
    request<BuddyAvailabilityDto>(`/local-buddies/${userId}/availability`, {
      method: "POST",
      body: data,
    }),

  updateBuddyAvailability: (availabilityId: string, data: BuddyAvailabilityRequest) =>
    request<BuddyAvailabilityDto>(`/buddy-availability/${availabilityId}`, {
      method: "PUT",
      body: data,
    }),

  setBuddyAvailabilityActive: (availabilityId: string, active: boolean) =>
    request<BuddyAvailabilityDto>(
      `/buddy-availability/${availabilityId}/${active ? "activate" : "deactivate"}`,
      { method: "PATCH" },
    ),

  createBuddyBooking: (userId: string, availabilityId: string, notes?: string) =>
    request<BuddyBookingDto>(`/local-buddies/${userId}/bookings`, {
      method: "POST",
      body: { availabilityId, notes },
    }),

  getMyBuddyBookings: () =>
    request<BuddyBookingDto[]>("/buddy-bookings/my"),

  getBuddyBookings: (userId: string) =>
    request<BuddyBookingDto[]>(`/local-buddies/${userId}/bookings`),

  updateBuddyBookingStatus: (bookingId: string, status: BuddyBookingStatus) =>
    request<BuddyBookingDto>(`/buddy-bookings/${bookingId}/status`, {
      method: "PATCH",
      body: { status },
    }),

  cancelBuddyBooking: (bookingId: string) =>
    request<BuddyBookingDto>(`/buddy-bookings/${bookingId}/cancel`, { method: "PATCH" }),

  getBuddyReviews: (userId: string) =>
    request<BuddyReviewDto[]>(`/local-buddies/${userId}/reviews`),

  createBuddyReview: (
    userId: string,
    data: { bookingId: string; rating: number; reviewText: string },
  ) => request<BuddyReviewDto>(`/local-buddies/${userId}/reviews`, {
    method: "POST",
    body: data,
  }),
};
