import { request } from "@/shared/lib/api-client";
import type {
  AdminUserListItem,
  AdminUserResponse,
  AdminRoleResponse,
  AdminAssignRoleRequest,
  AdminChangeUserStatusRequest,
  AdminReviewUserRegistrationRequest,
  AdminGetExperiencesRequest,
  AdminModerateExperienceRequest,
  AdminUpdateBuddyVerificationRequest,
  ExperienceSummaryDto,
  AdminLocalBuddy,
  AdminDashboard,
  AdminAnalytics,
  AdminAuditPage,
  BuddyVerificationEvent,
} from "@/shared/types/api";

export const adminApi = {
  getUsers: () =>
    request<AdminUserListItem[]>("/admin/users"),

  getUserById: (userId: string) =>
    request<AdminUserResponse>(`/admin/users/${userId}`),

  getRoles: () =>
    request<AdminRoleResponse[]>("/admin/users/roles"),

  assignRole: (userId: string, data: AdminAssignRoleRequest) =>
    request<AdminUserResponse>(`/admin/users/${userId}/roles`, { method: "POST", body: data }),

  changeUserStatus: (userId: string, data: AdminChangeUserStatusRequest) =>
    request<AdminUserResponse>(`/admin/users/${userId}/status`, { method: "PATCH", body: data }),

  reviewUserRegistration: (userId: string, data: AdminReviewUserRegistrationRequest) =>
    request<AdminUserResponse>(`/admin/users/${userId}/registration-review`, { method: "PATCH", body: data }),

  getExperiences: (params?: AdminGetExperiencesRequest) => {
    const query = params
      ? "?" + new URLSearchParams(
          Object.entries(params).reduce<Record<string, string>>((acc, [k, v]) => {
            if (v !== undefined && v !== null) acc[k] = String(v);
            return acc;
          }, {})
        ).toString()
      : "";
    return request<ExperienceSummaryDto[]>(`/admin/experiences${query}`);
  },

  moderateExperience: (id: number, data: AdminModerateExperienceRequest) =>
    request<ExperienceSummaryDto>(`/admin/experiences/${id}/moderation`, { method: "PATCH", body: data }),

  updateBuddyVerification: (userId: string, data: AdminUpdateBuddyVerificationRequest) =>
    request<AdminLocalBuddy>(`/admin/local-buddies/${userId}/verification`, { method: "PATCH", body: data }),

  getLocalBuddies: (verificationStatus?: string) =>
    request<AdminLocalBuddy[]>(
      `/admin/local-buddies${verificationStatus ? `?verificationStatus=${encodeURIComponent(verificationStatus)}` : ""}`,
    ),

  getBuddyVerificationHistory: (userId: string, page = 1, pageSize = 50) =>
    request<BuddyVerificationEvent[]>(
      `/admin/local-buddies/${userId}/verification-history?page=${page}&pageSize=${pageSize}`,
    ),

  getDashboard: () =>
    request<AdminDashboard>("/admin/dashboard"),

  getAnalytics: () =>
    request<AdminAnalytics>("/admin/analytics"),

  getAuditEvents: (params?: {
    actorUserId?: string;
    action?: string;
    fromUtc?: string;
    toUtc?: string;
    page?: number;
    pageSize?: number;
  }) => {
    const query = params
      ? `?${new URLSearchParams(
          Object.entries(params).reduce<Record<string, string>>((acc, [key, value]) => {
            if (value !== undefined) acc[key] = String(value);
            return acc;
          }, {}),
        ).toString()}`
      : "";
    return request<AdminAuditPage>(`/admin/audit-events${query}`);
  },
};
