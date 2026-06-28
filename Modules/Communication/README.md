# Communication Module

## Scope

The Communication module owns user-to-user messaging and in-app notifications.

Initial scope:

- Chat threads
- Chat participants
- Chat messages
- Notifications
- Notification preferences

## First Implementation Target

The first working slice should support:

- Creating or finding a direct chat thread between two users
- Sending a message in a thread
- Listing the current user's chat threads
- Reading thread messages
- Marking a thread as read
- Listing the current user's notifications
- Marking one or all notifications as read
- Managing per-user notification channel/category preferences
- Receiving persisted messages and read receipts through SignalR

## Boundaries

- This module should use authenticated user IDs from IdentityAccess.
- This module should not own profile data such as names, images, roles, or verification status.
- Other modules should create notifications through an application contract or service, not by writing to Communication tables directly.
- SignalR connections authenticate with the same JWT as REST. Clients join a
  thread group through `/hubs/chat`; participant membership is checked before
  subscription. Message writes remain on the rate-limited REST endpoint.

## Out Of Scope For The First Slice

- File and media attachments
- Message deletion or editing
- Push/email/SMS delivery (the preference flags are persisted for future providers)
- Message reactions
- Group chat management beyond the participant model needed by direct chats
- Cross-module workflows such as buddy booking chat automation
