import { request } from "@/shared/lib/api-client";
import type {
  TravelerProfileResponse,
  LocalBuddyListItemResponse,
  LocalBuddyProfileResponse,
  TravelerProfileRequest,
  InterestResponse,
  PagedResponse,
  FollowStatusResponse,
} from "@/shared/types/api";
import { buddyApi } from "@/shared/services/api-buddy";

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

export interface ProfileImageUploadDto {
  link: string;
  fileName: string;
  sizeBytes: number;
}

export interface ExperienceFavoriteStatus {
  experienceId: number;
  isFavorite: boolean;
}

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

  uploadProfileImage: (file: File) => {
    const body = new FormData();
    body.append("file", file);
    return request<ProfileImageUploadDto>("/profiles/images", {
      method: "POST",
      body,
    });
  },

  getExperienceFavoriteIds: () =>
    request<number[]>("/profiles/experience-favorites"),

  addExperienceFavorite: (experienceId: number) =>
    request<ExperienceFavoriteStatus>(`/profiles/experience-favorites/${experienceId}`, {
      method: "PUT",
    }),

  removeExperienceFavorite: (experienceId: number) =>
    request<ExperienceFavoriteStatus>(`/profiles/experience-favorites/${experienceId}`, {
      method: "DELETE",
    }),

  clearExperienceFavorites: () =>
    request<void>("/profiles/experience-favorites", { method: "DELETE" }),

  getLocalBuddies: getAllLocalBuddies,

  getLocalBuddy: (userId: string) =>
    request<LocalBuddyProfileResponse>(`/local-buddies/${userId}`),

  followUser: (userId: string) =>
    request<FollowStatusResponse>(`/profiles/users/${userId}/follow`, { method: "POST" }),

  unfollowUser: (userId: string) =>
    request<FollowStatusResponse>(`/profiles/users/${userId}/follow`, { method: "DELETE" }),

  getFollowStatus: (userId: string) =>
    request<FollowStatusResponse>(`/profiles/users/${userId}/follow-status`),

  // Compatibility aliases. New Buddy code imports buddyApi directly.
  getBuddyAvailability: buddyApi.getAvailability,
  getManagedBuddyAvailability: buddyApi.getManagedAvailability,
  createBuddyAvailability: buddyApi.createAvailability,
  updateBuddyAvailability: buddyApi.updateAvailability,
  setBuddyAvailabilityActive: buddyApi.setAvailabilityActive,
  createBuddyBooking: buddyApi.createRequest,
  getMyBuddyBookings: buddyApi.getMine,
  getBuddyBookings: (_userId: string) => buddyApi.getIncoming(),
  updateBuddyBookingStatus: (
    bookingId: string,
    status: "Accepted" | "Rejected" | "Completed",
  ) => status === "Accepted"
    ? buddyApi.accept(bookingId)
    : status === "Rejected"
      ? buddyApi.reject(bookingId)
      : request(`/buddy-bookings/${bookingId}/status`, {
          method: "PATCH",
          body: { status },
        }),
  cancelBuddyBooking: buddyApi.cancel,
  getBuddyReviews: buddyApi.getReviews,
  createBuddyReview: (
    _userId: string,
    data: { bookingId: string; rating: number; reviewText: string },
  ) => buddyApi.createReview(data.bookingId, {
    rating: data.rating,
    reviewText: data.reviewText,
  }),
};
