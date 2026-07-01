import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { adminApi } from "./api-admin";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("admin API", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("wires users, roles, and user status changes", async () => {
    await adminApi.getUsers();
    expect(request).toHaveBeenLastCalledWith("/admin/users");

    await adminApi.getRoles();
    expect(request).toHaveBeenLastCalledWith("/admin/users/roles");

    await adminApi.changeUserStatus("user-id", { isActive: false });
    expect(request).toHaveBeenLastCalledWith("/admin/users/user-id/status", {
      method: "PATCH",
      body: { isActive: false },
    });
  });

  it("wires buddy verification and experience moderation", async () => {
    await adminApi.getLocalBuddies("Pending");
    expect(request).toHaveBeenLastCalledWith("/admin/local-buddies?verificationStatus=Pending");

    await adminApi.updateBuddyVerification("user-id", {
      verificationStatus: "Approved",
    });
    expect(request).toHaveBeenLastCalledWith("/admin/local-buddies/user-id/verification", {
      method: "PATCH",
      body: { verificationStatus: "Approved" },
    });

    await adminApi.moderateExperience(12, { moderationStatus: "Approved" });
    expect(request).toHaveBeenLastCalledWith("/admin/experiences/12/moderation", {
      method: "PATCH",
      body: { moderationStatus: "Approved" },
    });
  });

  it("wires dashboard, analytics, and audit reads", async () => {
    await adminApi.getDashboard();
    expect(request).toHaveBeenLastCalledWith("/admin/dashboard");

    await adminApi.getAnalytics();
    expect(request).toHaveBeenLastCalledWith("/admin/analytics");

    await adminApi.getAuditEvents({ action: "AdminUsers.AssignRole", page: 1 });
    expect(request).toHaveBeenLastCalledWith(
      "/admin/audit-events?action=AdminUsers.AssignRole&page=1",
    );
  });

  it("loads buddy verification history", async () => {
    vi.mocked(request).mockResolvedValueOnce([]);
    await adminApi.getBuddyVerificationHistory("buddy-id", 2, 25);
    expect(request).toHaveBeenCalledWith(
      "/admin/local-buddies/buddy-id/verification-history?page=2&pageSize=25",
    );
  });
});
