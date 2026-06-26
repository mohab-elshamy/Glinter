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

## Boundaries

- This module should use authenticated user IDs from IdentityAccess.
- This module should not own profile data such as names, images, roles, or verification status.
- Other modules should create notifications through an application contract or service, not by writing to Communication tables directly.
- Real-time delivery with SignalR is a later enhancement after the REST flow is stable.

## Out Of Scope For The First Slice

- File and media attachments
- Message deletion or editing
- Push/email/SMS delivery
- Message reactions
- Group chat management beyond the participant model needed by direct chats
- Cross-module workflows such as buddy booking chat automation
