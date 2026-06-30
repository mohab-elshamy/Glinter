import { beforeEach, describe, expect, it, vi } from "vitest";
import { request } from "@/shared/lib/api-client";
import { chatApi, notificationsApi } from "./api-communication";

vi.mock("@/shared/lib/api-client", () => ({
  request: vi.fn(),
}));

describe("communication API", () => {
  beforeEach(() => {
    vi.mocked(request).mockReset();
  });

  it("wires chat threads, messages, and read state", async () => {
    await chatApi.getThreads();
    expect(request).toHaveBeenLastCalledWith("/chat/threads?page=1&pageSize=100");

    await chatApi.createDirectThread("other-user");
    expect(request).toHaveBeenLastCalledWith("/chat/threads/direct", {
      method: "POST",
      body: { otherUserId: "other-user" },
    });

    await chatApi.sendMessage("thread-id", "Hello");
    expect(request).toHaveBeenLastCalledWith("/chat/threads/thread-id/messages", {
      method: "POST",
      body: { body: "Hello" },
    });

    await chatApi.markThreadAsRead("thread-id");
    expect(request).toHaveBeenLastCalledWith("/chat/threads/thread-id/read", {
      method: "PATCH",
    });
  });

  it("wires notification unread state", async () => {
    await notificationsApi.getNotifications();
    expect(request).toHaveBeenLastCalledWith("/notifications?page=1&pageSize=100");

    await notificationsApi.markAsRead("notification-id");
    expect(request).toHaveBeenLastCalledWith("/notifications/notification-id/read", {
      method: "PATCH",
    });

    await notificationsApi.markAllAsRead();
    expect(request).toHaveBeenLastCalledWith("/notifications/read-all", {
      method: "PATCH",
    });
  });

  it("wires all notification preferences", async () => {
    const preferences = {
      inAppEnabled: true,
      emailEnabled: false,
      pushEnabled: true,
      chatMessageNotificationsEnabled: true,
      systemNotificationsEnabled: false,
    };

    await notificationsApi.updatePreferences(preferences);
    expect(request).toHaveBeenLastCalledWith("/notifications/preferences", {
      method: "PUT",
      body: preferences,
    });
  });
});
