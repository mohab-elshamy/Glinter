import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  ArrowLeft,
  Bell,
  CheckCheck,
  MessageSquare,
  Send,
  Settings,
} from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import Navbar from "@/components/Navbar";
import { toast } from "sonner";
import { authStorage } from "@/shared/lib/auth";
import { chatApi, notificationsApi } from "@/shared/services/api-communication";
import { ChatRealtimeClient } from "@/shared/services/chat-realtime";
import type {
  ChatMessageDto,
  ChatMessageEventDto,
  ChatThreadDto,
  NotificationDto,
  NotificationPreferencesDto,
} from "@/shared/types/api";
import type { LoadState } from "@/shared/types/async-state";
import { getSafeNotificationLink } from "@/shared/lib/notification-links";

interface SelectedBuddyState {
  userId?: string;
  name?: string;
}

const errorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Communication request failed.";

const MessagesPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const currentUserId = authStorage.getUser()?.userId ?? "";
  const [activeTab, setActiveTab] = useState<"notifications" | "messages">("notifications");
  const [threads, setThreads] = useState<ChatThreadDto[]>([]);
  const [threadPage, setThreadPage] = useState(1);
  const [hasMoreThreads, setHasMoreThreads] = useState(false);
  const [loadingMoreThreads, setLoadingMoreThreads] = useState(false);
  const threadPageSize = 20;
  const [messagesByThread, setMessagesByThread] = useState<Record<string, ChatMessageDto[]>>({});
  const [messagePageByThread, setMessagePageByThread] = useState<Record<string, number>>({});
  const [hasMoreMessagesByThread, setHasMoreMessagesByThread] = useState<Record<string, boolean>>({});
  const [loadingOlderMessages, setLoadingOlderMessages] = useState(false);
  const messagePageSize = 50;
  const [selectedThreadId, setSelectedThreadId] = useState<string>();
  const selectedThreadIdRef = useRef<string>();
  const [messageInput, setMessageInput] = useState("");
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [notificationUnreadCount, setNotificationUnreadCount] = useState(0);
  const [notificationPage, setNotificationPage] = useState(1);
  const notificationPageRef = useRef(1);
  const [notificationTotalCount, setNotificationTotalCount] = useState(0);
  const notificationPageSize = 20;
  const [preferences, setPreferences] = useState<NotificationPreferencesDto>();
  const [showPreferences, setShowPreferences] = useState(false);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const realtimeRef = useRef<ChatRealtimeClient>();
  const processedNotificationLinkRef = useRef("");

  useEffect(() => {
    selectedThreadIdRef.current = selectedThreadId;
  }, [selectedThreadId]);

  useEffect(() => {
    notificationPageRef.current = notificationPage;
  }, [notificationPage]);

  const mergeMessage = useCallback((message: ChatMessageDto) => {
    setMessagesByThread((current) => {
      const existing = current[message.threadId] ?? [];
      if (existing.some((item) => item.id === message.id)) return current;
      return {
        ...current,
        [message.threadId]: [...existing, message]
          .sort((a, b) => Date.parse(a.sentAtUtc) - Date.parse(b.sentAtUtc)),
      };
    });
  }, []);

  const onRealtimeMessage = useCallback((event: ChatMessageEventDto) => {
    const isSelected = selectedThreadIdRef.current === event.threadId;
    mergeMessage({
      ...event,
      isMine: event.senderUserId === currentUserId,
    });
    setThreads((current) =>
      current
        .map((thread) => thread.id === event.threadId
          ? {
              ...thread,
              lastMessageBody: event.body,
              lastMessageSenderUserId: event.senderUserId,
              lastMessageAtUtc: event.sentAtUtc,
              unreadCount: isSelected || event.senderUserId === currentUserId
                ? 0
                : thread.unreadCount + 1,
            }
          : thread)
        .sort((a, b) =>
          Date.parse(b.lastMessageAtUtc ?? b.createdAtUtc) -
          Date.parse(a.lastMessageAtUtc ?? a.createdAtUtc)),
    );
    if (isSelected && event.senderUserId !== currentUserId) {
      void chatApi.markThreadAsRead(event.threadId);
    }
  }, [currentUserId, mergeMessage]);

  const onRealtimeNotification = useCallback((notification: NotificationDto) => {
    setNotificationUnreadCount((count) => count + 1);
    setNotificationTotalCount((count) => count + 1);
    if (notificationPageRef.current === 1) {
      setNotifications((current) =>
        current.some((item) => item.id === notification.id)
          ? current
          : [notification, ...current].slice(0, notificationPageSize),
      );
    }
  }, []);

  const loadCommunication = useCallback(async () => {
    try {
      const [loadedThreads, notificationPage, loadedPreferences] = await Promise.all([
        chatApi.getThreads(1, threadPageSize),
        notificationsApi.getNotifications(1, notificationPageSize),
        notificationsApi.getPreferences(),
      ]);
      setThreads(loadedThreads);
      setThreadPage(1);
      setHasMoreThreads(loadedThreads.length === threadPageSize);
      setNotifications(notificationPage.items);
      setNotificationUnreadCount(notificationPage.unreadCount);
      setNotificationTotalCount(notificationPage.totalCount);
      setNotificationPage(notificationPage.page);
      setPreferences(loadedPreferences);
      setLoadState({ status: "ready" });
      return loadedThreads;
    } catch (error) {
      const message = errorMessage(error);
      setLoadState({ status: "error", message });
      toast.error(message);
      return [];
    }
  }, []);

  useEffect(() => {
    const realtime = new ChatRealtimeClient({
      messageReceived: onRealtimeMessage,
      threadRead: () => undefined,
      notificationReceived: onRealtimeNotification,
      reconnected: () => toast.info("Chat reconnected."),
    });
    realtimeRef.current = realtime;

    void loadCommunication().then(async (loadedThreads) => {
      try {
        await realtime.start();
        await Promise.all(loadedThreads.map((thread) => realtime.joinThread(thread.id)));
      } catch (error) {
        toast.error(`Realtime chat unavailable: ${errorMessage(error)}`);
      }
    });

    return () => {
      realtimeRef.current = undefined;
      void realtime.stop();
    };
  }, [loadCommunication, onRealtimeMessage, onRealtimeNotification]);

  useEffect(() => {
    const buddy = (location.state as { selectedBuddy?: SelectedBuddyState } | null)?.selectedBuddy;
    if (!buddy) return;
    if (!buddy.userId) {
      toast.error("This buddy is not connected to a backend user account.");
      return;
    }

    void chatApi.createDirectThread(buddy.userId)
      .then(async (created) => {
        const refreshed = await chatApi.getThreads(1, threadPageSize);
        setThreads(refreshed);
        setThreadPage(1);
        setHasMoreThreads(refreshed.length === threadPageSize);
        await realtimeRef.current?.joinThread(created.id);
        await openThread(created.id);
        setActiveTab("messages");
      })
      .catch((error: unknown) => toast.error(errorMessage(error)));
    // Process navigation state only once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.key]);

  const openThread = async (threadId: string) => {
    try {
      const loadedMessages = await chatApi.getMessages(threadId, 1, messagePageSize);
      setMessagesByThread((current) => ({ ...current, [threadId]: loadedMessages }));
      setMessagePageByThread((current) => ({ ...current, [threadId]: 1 }));
      setHasMoreMessagesByThread((current) => ({
        ...current,
        [threadId]: loadedMessages.length === messagePageSize,
      }));
      setSelectedThreadId(threadId);
      selectedThreadIdRef.current = threadId;
      setThreads((current) =>
        current.map((thread) => thread.id === threadId
          ? { ...thread, unreadCount: 0 }
          : thread),
      );
      await realtimeRef.current?.joinThread(threadId);
      await chatApi.markThreadAsRead(threadId);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const loadMoreThreads = async () => {
    if (loadingMoreThreads || !hasMoreThreads) return;
    const nextPage = threadPage + 1;
    setLoadingMoreThreads(true);
    try {
      const loaded = await chatApi.getThreads(nextPage, threadPageSize);
      setThreads((current) => {
        const known = new Set(current.map((thread) => thread.id));
        return [...current, ...loaded.filter((thread) => !known.has(thread.id))];
      });
      setThreadPage(nextPage);
      setHasMoreThreads(loaded.length === threadPageSize);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setLoadingMoreThreads(false);
    }
  };

  const loadOlderMessages = async () => {
    if (
      !selectedThreadId ||
      loadingOlderMessages ||
      !hasMoreMessagesByThread[selectedThreadId]
    ) return;

    const nextPage = (messagePageByThread[selectedThreadId] ?? 1) + 1;
    setLoadingOlderMessages(true);
    try {
      const loaded = await chatApi.getMessages(selectedThreadId, nextPage, messagePageSize);
      setMessagesByThread((current) => {
        const existing = current[selectedThreadId] ?? [];
        const known = new Set(existing.map((message) => message.id));
        return {
          ...current,
          [selectedThreadId]: [...loaded.filter((message) => !known.has(message.id)), ...existing]
            .sort((left, right) => Date.parse(left.sentAtUtc) - Date.parse(right.sentAtUtc)),
        };
      });
      setMessagePageByThread((current) => ({ ...current, [selectedThreadId]: nextPage }));
      setHasMoreMessagesByThread((current) => ({
        ...current,
        [selectedThreadId]: loaded.length === messagePageSize,
      }));
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setLoadingOlderMessages(false);
    }
  };

  useEffect(() => {
    if (loadState.status !== "ready") return;
    if (!location.search) {
      processedNotificationLinkRef.current = "";
      return;
    }
    const candidate = `/messages${location.search}`;
    if (processedNotificationLinkRef.current === candidate) return;
    const safeLink = getSafeNotificationLink(candidate);
    const threadId = new URLSearchParams(location.search).get("thread");
    processedNotificationLinkRef.current = candidate;
    if (!safeLink || !threadId) {
      toast.error("This notification link is invalid.");
      navigate("/messages", { replace: true });
      return;
    }
    if (!threads.some((thread) => thread.id === threadId)) {
      toast.error("That conversation is unavailable.");
      navigate("/messages", { replace: true });
      return;
    }
    setActiveTab("messages");
    void openThread(threadId);
  }, [loadState.status, location.search, navigate, threads]);

  const loadNotificationPage = async (page: number) => {
    try {
      const result = await notificationsApi.getNotifications(page, notificationPageSize);
      setNotifications(result.items);
      setNotificationUnreadCount(result.unreadCount);
      setNotificationTotalCount(result.totalCount);
      setNotificationPage(result.page);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const sendMessage = async () => {
    const body = messageInput.trim();
    if (!selectedThreadId || !body) return;
    setMessageInput("");
    try {
      const sent = await chatApi.sendMessage(selectedThreadId, body);
      mergeMessage(sent);
      setThreads((current) => current.map((thread) =>
        thread.id === selectedThreadId
          ? {
              ...thread,
              lastMessageBody: sent.body,
              lastMessageSenderUserId: sent.senderUserId,
              lastMessageAtUtc: sent.sentAtUtc,
              unreadCount: 0,
            }
          : thread));
    } catch (error) {
      setMessageInput(body);
      toast.error(errorMessage(error));
    }
  };

  const markNotificationRead = async (notification: NotificationDto) => {
    if (!notification.isRead) {
      try {
        const updated = await notificationsApi.markAsRead(notification.id);
        setNotifications((current) =>
          current.map((item) => item.id === updated.id ? updated : item),
        );
        setNotificationUnreadCount((count) => Math.max(0, count - 1));
      } catch (error) {
        toast.error(errorMessage(error));
        return;
      }
    }
    if (notification.linkUrl) {
      const safeLink = getSafeNotificationLink(notification.linkUrl);
      if (safeLink) navigate(safeLink);
      else toast.error("This notification link is invalid.");
    }
  };

  const markAllNotificationsRead = async () => {
    try {
      await notificationsApi.markAllAsRead();
      const readAtUtc = new Date().toISOString();
      setNotifications((current) =>
        current.map((item) => ({ ...item, isRead: true, readAtUtc })),
      );
      setNotificationUnreadCount(0);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const updatePreference = async (
    key: keyof Omit<NotificationPreferencesDto, "updatedAtUtc">,
  ) => {
    if (!preferences) return;
    const next = { ...preferences, [key]: !preferences[key] };
    try {
      const saved = await notificationsApi.updatePreferences({
        inAppEnabled: next.inAppEnabled,
        emailEnabled: next.emailEnabled,
        pushEnabled: next.pushEnabled,
        chatMessageNotificationsEnabled: next.chatMessageNotificationsEnabled,
        systemNotificationsEnabled: next.systemNotificationsEnabled,
      });
      setPreferences(saved);
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const selectedThread = threads.find((thread) => thread.id === selectedThreadId);
  const selectedMessages = selectedThreadId ? messagesByThread[selectedThreadId] ?? [] : [];
  const chatUnreadCount = threads.reduce((total, thread) => total + thread.unreadCount, 0);
  const sortedThreads = useMemo(
    () => [...threads].sort((a, b) =>
      Date.parse(b.lastMessageAtUtc ?? b.createdAtUtc) -
      Date.parse(a.lastMessageAtUtc ?? a.createdAtUtc)),
    [threads],
  );

  const otherParticipant = (thread: ChatThreadDto) => {
    const userId = thread.participantUserIds.find((id) => id !== currentUserId);
    return userId
      ? thread.participantDisplayNames[userId] ?? `User ${userId.slice(0, 8)}`
      : thread.title ?? "Conversation";
  };

  useEffect(() => {
    window.dispatchEvent(new CustomEvent("communication-unread-change", {
      detail: notificationUnreadCount,
    }));
  }, [notificationUnreadCount]);

  const notificationTotalPages = Math.max(
    1,
    Math.ceil(notificationTotalCount / notificationPageSize),
  );

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main className="container mx-auto max-w-5xl px-4 py-8">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Notifications & Messages</h1>
            <p className="text-sm text-muted-foreground">Persisted conversations and account updates</p>
          </div>
          <button onClick={() => setShowPreferences((value) => !value)} className="rounded-lg border border-border p-2" title="Notification preferences">
            <Settings className="h-4 w-4" />
          </button>
        </div>

        {showPreferences && preferences && (
          <section className="card-glass mb-6 p-5">
            <h2 className="mb-3 font-semibold">Notification preferences</h2>
            <div className="grid gap-3 sm:grid-cols-2">
              {([
                ["inAppEnabled", "In-app notifications"],
                ["chatMessageNotificationsEnabled", "Chat messages"],
                ["systemNotificationsEnabled", "System updates"],
              ] as const).map(([key, label]) => (
                <label key={key} className="flex items-center justify-between rounded-lg bg-secondary/30 p-3 text-sm">
                  {label}
                  <input type="checkbox" checked={preferences[key]} onChange={() => void updatePreference(key)} />
                </label>
              ))}
              <div className="rounded-lg border border-border bg-secondary/20 p-3 text-sm">
                <p className="font-medium">Email notifications</p>
                <p className="mt-1 text-xs text-muted-foreground">Unavailable until an email delivery provider is configured.</p>
              </div>
              <div className="rounded-lg border border-border bg-secondary/20 p-3 text-sm">
                <p className="font-medium">Push notifications</p>
                <p className="mt-1 text-xs text-muted-foreground">Unavailable until browser/device push delivery is configured.</p>
              </div>
            </div>
          </section>
        )}

        <div className="mb-6 flex max-w-sm rounded-lg bg-secondary p-1">
          <button onClick={() => { setActiveTab("notifications"); setSelectedThreadId(undefined); }} className={`flex-1 rounded-md py-2 text-xs ${activeTab === "notifications" ? "bg-accent text-accent-foreground" : ""}`}>
            <Bell className="mr-1 inline h-3.5 w-3.5" /> Notifications {notificationUnreadCount > 0 && `(${notificationUnreadCount})`}
          </button>
          <button onClick={() => setActiveTab("messages")} className={`flex-1 rounded-md py-2 text-xs ${activeTab === "messages" ? "bg-accent text-accent-foreground" : ""}`}>
            <MessageSquare className="mr-1 inline h-3.5 w-3.5" /> Messages {chatUnreadCount > 0 && `(${chatUnreadCount})`}
          </button>
        </div>

        {loadState.status === "loading" ? (
          <div className="card-glass p-12 text-center text-sm text-muted-foreground">Loading communication…</div>
        ) : loadState.status === "error" ? (
          <div className="rounded-xl border border-destructive/30 bg-destructive/10 p-5 text-sm text-destructive">
            {loadState.message}
          </div>
        ) : activeTab === "notifications" ? (
          <section className="card-glass p-5">
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="font-semibold">Notifications</h2>
                <p className="text-[11px] text-muted-foreground">
                  {notificationTotalCount} total · page {notificationPage} of {notificationTotalPages}
                </p>
              </div>
              {notificationUnreadCount > 0 && <button onClick={() => void markAllNotificationsRead()} className="text-xs text-accent"><CheckCheck className="mr-1 inline h-3 w-3" /> Mark all read</button>}
            </div>
            <div className="space-y-2">
              {notifications.length === 0 && <p className="py-8 text-center text-sm text-muted-foreground">No notifications yet.</p>}
              {notifications.map((notification) => (
                <button key={notification.id} onClick={() => void markNotificationRead(notification)} className={`w-full rounded-lg p-4 text-left ${notification.isRead ? "bg-secondary/20" : "border-l-4 border-accent bg-secondary/50"}`}>
                  <div className="flex flex-col justify-between gap-2 sm:flex-row sm:gap-3">
                    <div><p className="text-sm font-semibold">{notification.title}</p><p className="mt-1 text-xs text-muted-foreground">{notification.body}</p></div>
                    <span className="shrink-0 text-[10px] text-muted-foreground">{new Date(notification.createdAtUtc).toLocaleString()}</span>
                  </div>
                </button>
              ))}
            </div>
            {notificationTotalPages > 1 && (
              <nav aria-label="Notification pages" className="mt-5 flex items-center justify-center gap-3">
                <button
                  type="button"
                  disabled={notificationPage <= 1}
                  onClick={() => void loadNotificationPage(notificationPage - 1)}
                  className="rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-40"
                >
                  Previous
                </button>
                <span className="text-xs text-muted-foreground">
                  Page {notificationPage} of {notificationTotalPages}
                </span>
                <button
                  type="button"
                  disabled={notificationPage >= notificationTotalPages}
                  onClick={() => void loadNotificationPage(notificationPage + 1)}
                  className="rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-40"
                >
                  Next
                </button>
              </nav>
            )}
          </section>
        ) : selectedThread ? (
          <section className="card-glass flex h-[70vh] flex-col overflow-hidden">
            <header className="flex items-center gap-3 border-b border-border p-4">
              <button onClick={() => setSelectedThreadId(undefined)}><ArrowLeft className="h-5 w-5" /></button>
              <div><h2 className="font-semibold">{otherParticipant(selectedThread)}</h2><p className="text-xs text-muted-foreground">Realtime chat</p></div>
            </header>
            <div className="flex-1 space-y-3 overflow-y-auto p-4">
              {hasMoreMessagesByThread[selectedThread.id] && (
                <div className="text-center">
                  <button
                    type="button"
                    disabled={loadingOlderMessages}
                    onClick={() => void loadOlderMessages()}
                    className="rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-50"
                  >
                    {loadingOlderMessages ? "Loading…" : "Load older messages"}
                  </button>
                </div>
              )}
              {selectedMessages.length === 0 && <p className="py-10 text-center text-sm text-muted-foreground">No messages yet. Say hello.</p>}
              {selectedMessages.map((message) => (
                <div key={message.id} className={`flex ${message.isMine ? "justify-end" : "justify-start"}`}>
                  <div className={`max-w-[75%] rounded-xl p-3 ${message.isMine ? "bg-accent text-accent-foreground" : "bg-secondary"}`}>
                    <p className="text-sm">{message.body}</p>
                    <p className="mt-1 text-[10px] opacity-70">{new Date(message.sentAtUtc).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</p>
                  </div>
                </div>
              ))}
            </div>
            <footer className="flex gap-2 border-t border-border p-4">
              <input value={messageInput} onChange={(event) => setMessageInput(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter") void sendMessage(); }} className="input-glass flex-1" maxLength={4000} placeholder="Type a message" />
              <button onClick={() => void sendMessage()} disabled={!messageInput.trim()} className="rounded-lg bg-accent p-2 disabled:opacity-50"><Send className="h-5 w-5" /></button>
            </footer>
          </section>
        ) : (
          <section className="card-glass overflow-hidden">
            <header className="border-b border-border p-4"><h2 className="font-semibold">Conversations</h2></header>
            {sortedThreads.length === 0 ? (
              <p className="p-10 text-center text-sm text-muted-foreground">No conversations yet. Open an approved local buddy profile to start one.</p>
            ) : sortedThreads.map((thread) => (
              <button key={thread.id} onClick={() => void openThread(thread.id)} className="flex w-full items-center justify-between border-b border-border p-4 text-left last:border-0 hover:bg-secondary/30">
                <div className="min-w-0"><p className="font-medium">{otherParticipant(thread)}</p><p className="truncate text-xs text-muted-foreground">{thread.lastMessageBody || "New conversation"}</p></div>
                <div className="ml-3 text-right">
                  {thread.lastMessageAtUtc && <p className="text-[10px] text-muted-foreground">{new Date(thread.lastMessageAtUtc).toLocaleString()}</p>}
                  {thread.unreadCount > 0 && <span className="mt-1 inline-block rounded-full bg-accent px-2 py-0.5 text-[10px]">{thread.unreadCount}</span>}
                </div>
              </button>
            ))}
            {hasMoreThreads && (
              <div className="border-t border-border p-4 text-center">
                <button
                  type="button"
                  disabled={loadingMoreThreads}
                  onClick={() => void loadMoreThreads()}
                  className="rounded-lg border border-border px-4 py-2 text-xs disabled:opacity-50"
                >
                  {loadingMoreThreads ? "Loading…" : "Load more conversations"}
                </button>
              </div>
            )}
          </section>
        )}
      </main>
    </div>
  );
};

export default MessagesPage;
