import { request } from "@/shared/lib/api-client";
import type {
  AdminUserListItem,
  AdminRoleResponse,
  AdminAssignRoleRequest,
  AdminChangeUserStatusRequest,
  AdminGetExperiencesRequest,
  AdminSetApprovalStatusRequest,
  AdminUpdateBuddyVerificationRequest,
  ExperienceSummaryDto,
} from "@/shared/types/api";

export const adminApi = {
  getUsers: () =>
    request<AdminUserListItem[]>("/admin/users"),

  getRoles: () =>
    request<AdminRoleResponse[]>("/admin/users/roles"),

  assignRole: (userId: string, data: AdminAssignRoleRequest) =>
    request<AdminUserListItem>(`/admin/users/${userId}/roles`, { method: "POST", body: data }),

  changeUserStatus: (userId: string, data: AdminChangeUserStatusRequest) =>
    request<AdminUserListItem>(`/admin/users/${userId}/status`, { method: "PATCH", body: data }),

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

  setExperienceApprovalStatus: (id: string, data: AdminSetApprovalStatusRequest) =>
    request<ExperienceSummaryDto>(`/admin/experiences/${id}/approval-status`, { method: "PATCH", body: data }),

  updateBuddyVerification: (userId: string, data: AdminUpdateBuddyVerificationRequest) =>
    request<void>(`/admin/local-buddies/${userId}/verification`, { method: "PATCH", body: data }),
};
