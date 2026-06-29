import { request } from "@/shared/lib/api-client";
import type {
  TravelerProfileResponse,
  LocalBuddyListItemResponse,
  LocalBuddyProfileResponse,
  TravelerProfileRequest,
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

export const profilesApi = {
  getMyProfile: () =>
    request<MyProfileResponse>("/profiles/me"),

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

  getLocalBuddies: () =>
    request<LocalBuddyListItemResponse[]>("/local-buddies"),

  getLocalBuddy: (userId: string) =>
    request<LocalBuddyProfileResponse>(`/local-buddies/${userId}`),

  followUser: (userId: string) =>
    request<void>(`/profiles/users/${userId}/follow`, { method: "POST" }),

  unfollowUser: (userId: string) =>
    request<void>(`/profiles/users/${userId}/follow`, { method: "DELETE" }),

  getFollowStatus: (userId: string) =>
    request<{ isFollowing: boolean }>(`/profiles/users/${userId}/follow-status`),
};
