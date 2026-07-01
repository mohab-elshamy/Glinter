import { request } from "@/shared/lib/api-client";
import type {
  ChatMessageDto,
  ChatThreadDto,
  NotificationDto,
  NotificationPreferencesDto,
  NotificationsPageDto,
  UpdateNotificationPreferencesRequest,
} from "@/shared/types/api";

export const chatApi = {
  getThreads: (page = 1, pageSize = 100) =>
    request<ChatThreadDto[]>(`/chat/threads?page=${page}&pageSize=${pageSize}`),

  createDirectThread: (otherUserId: string) =>
    request<ChatThreadDto>("/chat/threads/direct", {
      method: "POST",
      body: { otherUserId },
    }),

  getMessages: (threadId: string, page = 1, pageSize = 100) =>
    request<ChatMessageDto[]>(
      `/chat/threads/${threadId}/messages?page=${page}&pageSize=${pageSize}`,
    ),

  sendMessage: (threadId: string, body: string) =>
    request<ChatMessageDto>(`/chat/threads/${threadId}/messages`, {
      method: "POST",
      body: { body },
    }),

  markThreadAsRead: (threadId: string) =>
    request<void>(`/chat/threads/${threadId}/read`, { method: "PATCH" }),
};

export const notificationsApi = {
  getNotifications: (page = 1, pageSize = 100) =>
    request<NotificationsPageDto>(`/notifications?page=${page}&pageSize=${pageSize}`),

  markAsRead: (notificationId: string) =>
    request<NotificationDto>(`/notifications/${notificationId}/read`, {
      method: "PATCH",
    }),

  markAllAsRead: () =>
    request<void>("/notifications/read-all", { method: "PATCH" }),

  getPreferences: () =>
    request<NotificationPreferencesDto>("/notifications/preferences"),

  updatePreferences: (data: UpdateNotificationPreferencesRequest) =>
    request<NotificationPreferencesDto>("/notifications/preferences", {
      method: "PUT",
      body: data,
    }),
};
