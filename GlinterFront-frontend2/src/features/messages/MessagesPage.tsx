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
  const [messagesByThread, setMessagesByThread] = useState<Record<string, ChatMessageDto[]>>({});
  const [selectedThreadId, setSelectedThreadId] = useState<string>();
  const selectedThreadIdRef = useRef<string>();
  const [messageInput, setMessageInput] = useState("");
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [notificationUnreadCount, setNotificationUnreadCount] = useState(0);
  const [preferences, setPreferences] = useState<NotificationPreferencesDto>();
  const [showPreferences, setShowPreferences] = useState(false);
  const [loadState, setLoadState] = useState<LoadState>({ status: "loading" });
  const realtimeRef = useRef<ChatRealtimeClient>();

  useEffect(() => {
    selectedThreadIdRef.current = selectedThreadId;
  }, [selectedThreadId]);

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

  const loadCommunication = useCallback(async () => {
    try {
      const [loadedThreads, notificationPage, loadedPreferences] = await Promise.all([
        chatApi.getThreads(),
        notificationsApi.getNotifications(),
        notificationsApi.getPreferences(),
      ]);
      setThreads(loadedThreads);
      setNotifications(notificationPage.items);
      setNotificationUnreadCount(notificationPage.unreadCount);
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
  }, [loadCommunication, onRealtimeMessage]);

  useEffect(() => {
    const buddy = (location.state as { selectedBuddy?: SelectedBuddyState } | null)?.selectedBuddy;
    if (!buddy) return;
    if (!buddy.userId) {
      toast.error("This buddy is not connected to a backend user account.");
      return;
    }

    void chatApi.createDirectThread(buddy.userId)
      .then(async (created) => {
        const refreshed = await chatApi.getThreads();
        setThreads(refreshed);
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
      const loadedMessages = await chatApi.getMessages(threadId);
      setMessagesByThread((current) => ({ ...current, [threadId]: loadedMessages }));
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
    if (notification.linkUrl) navigate(notification.linkUrl);
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
      detail: notificationUnreadCount + chatUnreadCount,
    }));
  }, [chatUnreadCount, notificationUnreadCount]);

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
                ["emailEnabled", "Email notifications"],
                ["pushEnabled", "Push notifications"],
                ["chatMessageNotificationsEnabled", "Chat messages"],
                ["systemNotificationsEnabled", "System updates"],
              ] as const).map(([key, label]) => (
                <label key={key} className="flex items-center justify-between rounded-lg bg-secondary/30 p-3 text-sm">
                  {label}
                  <input type="checkbox" checked={preferences[key]} onChange={() => void updatePreference(key)} />
                </label>
              ))}
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
              <h2 className="font-semibold">Notifications</h2>
              {notificationUnreadCount > 0 && <button onClick={() => void markAllNotificationsRead()} className="text-xs text-accent"><CheckCheck className="mr-1 inline h-3 w-3" /> Mark all read</button>}
            </div>
            <div className="space-y-2">
              {notifications.length === 0 && <p className="py-8 text-center text-sm text-muted-foreground">No notifications yet.</p>}
              {notifications.map((notification) => (
                <button key={notification.id} onClick={() => void markNotificationRead(notification)} className={`w-full rounded-lg p-4 text-left ${notification.isRead ? "bg-secondary/20" : "border-l-4 border-accent bg-secondary/50"}`}>
                  <div className="flex justify-between gap-3">
                    <div><p className="text-sm font-semibold">{notification.title}</p><p className="mt-1 text-xs text-muted-foreground">{notification.body}</p></div>
                    <span className="shrink-0 text-[10px] text-muted-foreground">{new Date(notification.createdAtUtc).toLocaleString()}</span>
                  </div>
                </button>
              ))}
            </div>
          </section>
        ) : selectedThread ? (
          <section className="card-glass flex h-[70vh] flex-col overflow-hidden">
            <header className="flex items-center gap-3 border-b border-border p-4">
              <button onClick={() => setSelectedThreadId(undefined)}><ArrowLeft className="h-5 w-5" /></button>
              <div><h2 className="font-semibold">{otherParticipant(selectedThread)}</h2><p className="text-xs text-muted-foreground">Realtime chat</p></div>
            </header>
            <div className="flex-1 space-y-3 overflow-y-auto p-4">
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
          </section>
        )}
      </main>
    </div>
  );
};

export default MessagesPage;
