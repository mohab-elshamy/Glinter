import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { API_ORIGIN } from "@/shared/lib/api-client";
import { authStorage } from "@/shared/lib/auth";
import type {
  ChatMessageEventDto,
  ChatThreadReadEventDto,
  NotificationDto,
} from "@/shared/types/api";

export interface ChatRealtimeHandlers {
  messageReceived?: (message: ChatMessageEventDto) => void;
  threadRead?: (event: ChatThreadReadEventDto) => void;
  notificationReceived?: (notification: NotificationDto) => void;
  reconnected?: () => void;
}

export class ChatRealtimeClient {
  private readonly connection: HubConnection;
  private joinedThreadIds = new Set<string>();

  constructor(handlers: ChatRealtimeHandlers) {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_ORIGIN}/hubs/chat`, {
        accessTokenFactory: () => authStorage.getToken() ?? "",
      })
      .withAutomaticReconnect([0, 2_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    if (handlers.messageReceived) {
      this.connection.on("MessageReceived", handlers.messageReceived);
    }
    if (handlers.threadRead) {
      this.connection.on("ThreadRead", handlers.threadRead);
    }
    if (handlers.notificationReceived) {
      this.connection.on("NotificationCreated", handlers.notificationReceived);
    }
    this.connection.onreconnected(async () => {
      const threadIds = [...this.joinedThreadIds];
      this.joinedThreadIds.clear();
      await Promise.all(threadIds.map((threadId) => this.joinThread(threadId)));
      handlers.reconnected?.();
    });
  }

  async start() {
    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async joinThread(threadId: string) {
    await this.start();
    if (!this.joinedThreadIds.has(threadId)) {
      await this.connection.invoke("JoinThread", threadId);
      this.joinedThreadIds.add(threadId);
    }
  }

  async leaveThread(threadId: string) {
    if (this.connection.state === HubConnectionState.Connected &&
        this.joinedThreadIds.has(threadId)) {
      await this.connection.invoke("LeaveThread", threadId);
    }
    this.joinedThreadIds.delete(threadId);
  }

  async stop() {
    this.joinedThreadIds.clear();
    await this.connection.stop();
  }
}
