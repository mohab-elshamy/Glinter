# Glinter Frontend API Integration Guide

Generated from the backend source and updated on 2026-06-28.

This is the frontend handoff for all 90 HTTP operations currently exposed by the project. It documents the implementation as it exists today, including authorization gaps that should be fixed before production integration.

## 1. Connection and protocol conventions

| Item | Value |
|---|---|
| Development HTTP base URL | `http://localhost:5160` |
| Development HTTPS base URL | `https://localhost:7209` |
| Swagger UI | `/swagger` |
| OpenAPI JSON | `/swagger/v1/swagger.json` |
| JSON media type | `application/json` |
| Authentication | JWT bearer token |
| Authorization header | `Authorization: Bearer <token>` |
| JSON naming | camelCase |
| UUID values | JSON strings, for example `"915b6a30-..."` |
| UTC timestamps | ISO 8601 strings, for example `2026-06-22T18:30:00Z` |
| Date-only values | `YYYY-MM-DD` |
| Decimal/money values | JSON numbers |

The access JWT expires after the configured `Jwt:ExpiryMinutes` value. Login
also returns a 30-day, opaque refresh token. Each refresh rotates that token;
reuse of an older token revokes the complete token family and invalidates its
access tokens. Logout revokes the current access token and all refresh tokens
for that user.

### Frontend HTTP client behavior

1. Send `Content-Type: application/json` for JSON bodies.
2. Attach the bearer token to every endpoint marked `Bearer` or with a role.
3. Treat `401` as unauthenticated/expired/revoked and clear the local session.
4. Treat `403` as authenticated but missing the required role or ownership.
5. Read failed responses as `application/problem+json`.
6. On refresh failure, clear the complete local session and require login.

## 2. Security behavior

Public registration cannot create Admin users. New accounts must confirm their
email before login. Admin login always requires authenticator MFA. Region
writes and ownership-sensitive stay, experience, communication, and profile
operations are enforced by the backend.

## 3. Roles and fixed string values

### Roles

Role strings are case-sensitive:

- `Traveler`
- `LocalBuddy`
- `HotelOwner`
- `ExperienceProvider`
- `Admin`

Public signup should expose only the first four roles.

### Password policy

- At least 10 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one non-alphanumeric character
- At least 4 unique characters

### Status values

```ts
type VerificationStatus = "Pending" | "Approved" | "Rejected";
type ExperienceApprovalStatus = "Pending" | "Approved" | "Rejected" | "Hidden";
type ExperienceBookingStatus = "Pending" | "Confirmed" | "Cancelled" | "Completed";
```

## 4. Identity Access module

### Endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `POST /api/auth/register` | Public | `RegisterRequest` body | `200 RegisterResponse` |
| `POST /api/auth/confirm-email` | Public | `ConfirmEmailRequest` body | `200 MessageResponse` |
| `POST /api/auth/resend-confirmation` | Public | `EmailRequest` body | `200 MessageResponse` |
| `POST /api/auth/login` | Public | `LoginRequest` body | `200 AuthResponse` or `MfaChallengeResponse` |
| `POST /api/auth/refresh` | Public | `RefreshTokenRequest` body | `200 AuthResponse` |
| `POST /api/auth/forgot-password` | Public | `EmailRequest` body | `200 MessageResponse` |
| `POST /api/auth/reset-password` | Public | `ResetPasswordRequest` body | `200 MessageResponse` |
| `POST /api/auth/mfa/setup` | Public + MFA ticket | `MfaTicketRequest` body | `200 MfaSetupResponse` |
| `POST /api/auth/mfa/enable` | Public + MFA ticket | `EnableMfaRequest` body | `200 EnableMfaResponse` |
| `POST /api/auth/mfa/verify` | Public + MFA ticket | `VerifyMfaRequest` body | `200 AuthResponse` |
| `GET /api/auth/me` | Bearer | None | `200 CurrentUserResponse` |
| `POST /api/auth/logout` | Bearer | None | `200 LogoutResponse`; token becomes invalid |
| `GET /api/admin/users` | Admin | None | `200 UserListItemResponse[]` |
| `GET /api/admin/users/roles` | Admin | None | `200 RoleResponse[]` |
| `GET /api/admin/users/{id}` | Admin | Path: `id: UUID` | `200 UserResponse` |
| `POST /api/admin/users/{id}/roles` | Admin | Path: `id`; `AssignRoleRequest` body | `200 UserResponse` |
| `PATCH /api/admin/users/{id}/status` | Admin | Path: `id`; `ChangeUserStatusRequest` body | `200 UserResponse` |

### Request bodies

```ts
interface RegisterRequest {
  fullName: string; // required
  email: string;    // required
  password: string; // required; must satisfy the password policy
  role: "Traveler" | "LocalBuddy" | "HotelOwner" | "ExperienceProvider";
}

interface LoginRequest {
  email: string;
  password: string;
}

interface ConfirmEmailRequest { userId: string; token: string; }
interface EmailRequest { email: string; }
interface RefreshTokenRequest { refreshToken: string; }
interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}
interface MfaTicketRequest { mfaTicket: string; }
interface EnableMfaRequest extends MfaTicketRequest { code: string; }
interface VerifyMfaRequest extends MfaTicketRequest {
  code?: string;
  recoveryCode?: string;
}

interface AssignRoleRequest {
  role: "Traveler" | "LocalBuddy" | "HotelOwner" | "ExperienceProvider" | "Admin";
}

interface ChangeUserStatusRequest {
  isActive: boolean;
}
```

### Response shapes

```ts
interface AuthResponse {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
  token: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

interface RegisterResponse {
  userId: string;
  email: string;
  message: string;
  developmentConfirmationToken?: string | null;
}

interface MessageResponse {
  message: string;
  developmentToken?: string | null;
}

interface MfaChallengeResponse {
  requiresMfa: true;
  requiresSetup: boolean;
  mfaTicket: string;
  expiresAtUtc: string;
}

interface MfaSetupResponse {
  sharedKey: string;
  authenticatorUri: string;
  mfaTicket: string;
  expiresAtUtc: string;
}

interface EnableMfaResponse {
  authentication: AuthResponse;
  recoveryCodes: string[];
}

interface CurrentUserResponse {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
}

interface LogoutResponse { message: string; }
interface RoleResponse { name: string; }

interface UserListItemResponse {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  role: string;
}

interface UserResponse {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAtUtc: string;
  roles: string[];
}
```

Expected failures include `400` for invalid input/duplicate email, `401` for invalid credentials/token, `403` for non-admin access, and `404` for an unknown user.

Confirmation/reset tokens are returned only in Development; deployed clients
receive them through email links. MFA tickets expire after five minutes and are
single-use. For first Admin login, call `mfa/setup`, show the URI/key in an
authenticator app, then call `mfa/enable` with the returned enable ticket.

## 5. Profiles module

### Endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/interests` | Public | None | `200 InterestResponse[]` |
| `GET /api/local-buddies` | Public | Query: `city?`, `search?`, `page=1`, `pageSize=10` (max 50) | `200 PagedResponse<LocalBuddyListItemResponse>` |
| `GET /api/local-buddies/{userId}` | Public | Path: `userId: UUID` | `200 LocalBuddyProfileResponse` |
| `GET /api/profiles/me` | Bearer | None | `200` one role-specific profile response |
| `PUT /api/profiles/traveler` | Traveler | `TravelerProfileRequest` body | `200 TravelerProfileResponse` |
| `PUT /api/profiles/local-buddy` | LocalBuddy | `LocalBuddyProfileRequest` body | `200 LocalBuddyProfileResponse` |
| `PUT /api/profiles/hotel-owner` | HotelOwner | `HotelOwnerProfileRequest` body | `200 HotelOwnerProfileResponse` |
| `PUT /api/profiles/experience-provider` | ExperienceProvider | `ExperienceProviderProfileRequest` body | `200 ExperienceProviderProfileResponse` |
| `PATCH /api/profiles/image` | Bearer | `UpdateProfileImageRequest` body | `200` updated role-specific profile response |
| `POST /api/profiles/users/{userId}/follow` | Bearer | Path: `userId: UUID` | `200 { message }` |
| `DELETE /api/profiles/users/{userId}/follow` | Bearer | Path: `userId: UUID` | `200 { message }` |
| `GET /api/profiles/users/{userId}/follow-status` | Bearer | Path: `userId: UUID` | `200 FollowStatusResponse` |
| `PATCH /api/admin/local-buddies/{userId}/verification` | Admin | Path: `userId`; `UpdateLocalBuddyVerificationRequest` body | `200 LocalBuddyProfileResponse` |
| `GET /api/admin/local-buddies/{userId}/verification-history` | Admin | Query: `page`, `pageSize` | `200 LocalBuddyVerificationEventDto[]` |

`PUT` profile endpoints are upserts: they create the role profile on first use and update it later.

### Request bodies

```ts
interface TravelerProfileRequest {
  displayName: string;          // required, max 200
  bio?: string | null;          // max 1000
  nationality?: string | null;  // max 100
  preferredBudgetLevel?: string | null; // max 50
  travelStyle?: string | null;  // max 100
  preferredInterests?: string | null; // max 1000
  interestIds: string[];        // all IDs must exist
}

interface LocalBuddyProfileRequest {
  displayName: string;  // required, max 200
  bio?: string | null;  // max 1000
  city: string;         // required, max 100
  languages?: string | null; // max 500
  interestIds: string[];     // all IDs must exist
}

interface HotelOwnerProfileRequest {
  businessName: string; // required, max 200
  contactPersonName?: string | null; // max 200
  phoneNumber?: string | null;       // max 50
  description?: string | null;       // max 1000
}

type ExperienceProviderProfileRequest = HotelOwnerProfileRequest;

interface UpdateProfileImageRequest {
  profileImageUrl: string; // required absolute HTTP/HTTPS URL, max 1000
}

interface UpdateLocalBuddyVerificationRequest {
  verificationStatus: VerificationStatus;
  moderationNotes?: string | null;
}
```

### Response shapes

```ts
interface InterestResponse { id: string; name: string; }
interface FollowStatusResponse { followedUserId: string; isFollowing: boolean; }

interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface LocalBuddyListItemResponse {
  profileId: string;
  userId: string;
  displayName: string;
  bio: string | null;
  city: string;
  languages: string | null;
  profileImageUrl: string | null;
  rating: number;
  reviewsCount: number;
  verificationStatus: VerificationStatus;
  interests: InterestResponse[];
  followersCount: number;
  followingCount: number;
}

interface TravelerProfileResponse {
  profileId: string;
  userId: string;
  profileType: "Traveler";
  displayName: string;
  bio: string | null;
  nationality: string | null;
  preferredBudgetLevel: string | null;
  travelStyle: string | null;
  preferredInterests: string | null;
  profileImageUrl: string | null;
  interests: InterestResponse[];
  followersCount: number;
  followingCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

interface LocalBuddyProfileResponse extends LocalBuddyListItemResponse {
  profileType: "LocalBuddy";
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

interface BusinessProfileResponse {
  profileId: string;
  userId: string;
  profileType: "HotelOwner" | "ExperienceProvider";
  businessName: string;
  contactPersonName: string | null;
  phoneNumber: string | null;
  description: string | null;
  profileImageUrl: string | null;
  followersCount: number;
  followingCount: number;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

type HotelOwnerProfileResponse = BusinessProfileResponse;
type ExperienceProviderProfileResponse = BusinessProfileResponse;
```

The user cannot follow themselves. Re-following an already-followed user is handled as a validation conflict.

## 6. Stays module

Paths generated from `StaysController` use capital `S` (`/api/Stays`). ASP.NET routing is case-insensitive, but the frontend should use the documented spelling consistently.

For create/update location selection, send an ADM3 neighbourhood `gid` as `adm3Gid`. The backend verifies that it exists and returns the complete country → governorate → district → neighbourhood hierarchy in `region`.

### Endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/Stays` | Public | Search and pagination query | `200 StaySummaryDto[]` |
| `GET /api/Stays/{id}` | Public | Path: `id: UUID` | `200 StayResponseDto` |
| `GET /api/Stays/by-neighbourhood/{adm3Gid}` | Public | Path: `adm3Gid: integer` | `200 StaySummaryDto[]` |
| `POST /api/Stays` | Bearer; handler requires HotelOwner profile | `CreateStayRequestDto` body | `201 StayResponseDto` |
| `PUT /api/Stays/{id}` | Public ⚠ | Path: `id`; `UpdateStayRequestDto` body | `200 StayResponseDto` |
| `PATCH /api/Stays/{id}/activate` | Public ⚠ | Path: `id` | `200 StayResponseDto` |
| `PATCH /api/Stays/{id}/deactivate` | Public ⚠ | Path: `id` | `200 StayResponseDto` |
| `POST /api/stays/{stayId}/bookings` | Bearer; handler requires Traveler profile | Path: `stayId`; `CreateStayBookingRequestDto` body | `200 StayBookingResponseDto` |
| `GET /api/stays/{stayId}/bookings` | Public ⚠ | Path: `stayId` | `200 StayBookingResponseDto[]` |
| `PATCH /api/stay-bookings/{bookingId}/cancel` | Public ⚠ | Path: `bookingId` | `200 StayBookingResponseDto` |
| `POST /api/stays/{stayId}/reviews` | Bearer; handler requires Traveler profile | Path: `stayId`; `CreateStayReviewRequestDto` body | `200 StayReviewResponseDto` |
| `GET /api/stays/{stayId}/reviews` | Public | Path: `stayId` | `200 StayReviewResponseDto[]` |
| `PUT /api/stay-reviews/{reviewId}` | Bearer; owning Traveler | Path: `reviewId`; `UpdateStayReviewRequestDto` body | `200 StayReviewResponseDto` |
| `DELETE /api/stay-reviews/{reviewId}` | Bearer; owning Traveler | Path: `reviewId` | `200 { message }` |

### Query rules

- `minPrice` and `maxPrice` cannot be negative.
- `minPrice` cannot exceed `maxPrice`.
- `guests`, when supplied, must be greater than zero.
- `adm3Gid`, when supplied, is an integer neighbourhood `gid` from the Regions module.
- `search` matches name, description, address, and tags; max 200 characters.
- `currency` is an exact, case-insensitive currency-code filter.
- `checkInDate` and `checkOutDate` must be supplied together; unavailable stays are excluded.
- `sortBy` accepts `newest`, `price_asc`, or `price_desc`.

### Request bodies

```ts
interface CreateStayRequestDto {
  adm3Gid: number;      // required; existing Regions neighbourhood gid
  name: string;         // required
  description: string;
  address: string;
  pricePerNight: number; // > 0
  currency: string;      // defaults to EGP
  maxGuests: number;     // > 0
  latitude: number;
  longitude: number;
  tags: string[];
}

interface UpdateStayRequestDto {
  name: string;          // required
  description?: string | null;
  address?: string | null;
  pricePerNight: number; // > 0
  currency: string;
  maxGuests: number;     // > 0
  latitude: number;
  longitude: number;
  tags?: string[] | null;
}

interface CreateStayBookingRequestDto {
  checkInDate: string;  // YYYY-MM-DD; today or future
  checkOutDate: string; // YYYY-MM-DD; after checkInDate
  guestCount: number;   // > 0 and <= stay.maxGuests
}

interface CreateStayReviewRequestDto {
  rating: number; // integer 1..5
  comment?: string | null;
}

interface UpdateStayReviewRequestDto {
  rating: number; // integer 1..5
  comment?: string | null;
  // travelerProfileId exists in the backend DTO but is ignored; do not send it.
}
```

Booking rules: the stay must be active, dates cannot overlap an existing booking, duplicate traveler/date bookings are rejected, and the stay must be booked for at least one night. A stay review requires an eligible completed/past booking and only one review per traveler/stay.

### Response shapes

```ts
interface StayTagDto { id: string; stayId: string; name: string; }

interface StaySummaryDto {
  id: string;
  adm3Gid: number;
  region: RegionReferenceDto | null;
  name: string;
  address: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  isActive: boolean;
}

interface StayResponseDto {
  id: string;
  ownerProfileId: string;
  adm3Gid: number;
  region: RegionReferenceDto | null;
  name: string;
  description: string;
  address: string;
  pricePerNight: number;
  currency: string;
  maxGuests: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  tags: StayTagDto[];
}

interface StayBookingResponseDto {
  id: string;
  stayId: string;
  travelerProfileId: string;
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
  totalPrice: number;
  status: string;
  createdAtUtc: string;
}

interface StayReviewResponseDto {
  id: string;
  stayId: string;
  travelerProfileId: string;
  rating: number;
  comment: string;
  createdAtUtc: string;
}
```

## 7. Experiences module

Experience location uses the same Regions contract as stays: `adm3Gid` is a numeric neighbourhood `gid`, and listing/detail responses include `region`.

### Experience listing and management

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/experiences` | Public | Search and pagination query | `200 ExperienceSummaryDto[]` |
| `GET /api/experiences/{id}` | Public | Path: `id: UUID` | `200 ExperienceResponseDto` |
| `GET /api/experiences/my` | ExperienceProvider | None | `200 ExperienceSummaryDto[]` |
| `POST /api/experiences` | ExperienceProvider | `CreateExperienceRequestDto` body | `201 ExperienceResponseDto` |
| `PUT /api/experiences/{id}` | ExperienceProvider; owner enforced | Path: `id`; `UpdateExperienceRequestDto` body | `200 ExperienceResponseDto` |
| `PATCH /api/experiences/{id}/activate` | ExperienceProvider; owner enforced | Path: `id` | `200 ExperienceResponseDto` |
| `PATCH /api/experiences/{id}/deactivate` | ExperienceProvider; owner enforced | Path: `id` | `200 ExperienceResponseDto` |
| `GET /api/experience-categories` | Public | None | `200 ExperienceCategoryResponseDto[]` |
| `GET /api/vibes` | Public | None | `200 VibeResponseDto[]` |

Listing query rules match stays. `search` matches title, description, location,
and tags. Additional filters are `currency`, `minDurationMinutes`,
`maxDurationMinutes`, and the paired `availableFromUtc`/`availableToUtc`
window. `sortBy` accepts `newest`, `price_asc`, `price_desc`, or
`duration_asc`. Availability filtering also checks remaining slot capacity
when `guests` is supplied.

### Availability

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/experiences/{experienceId}/availability` | Public | Path: `experienceId` | `200 ExperienceAvailabilityResponseDto[]` |
| `POST /api/experiences/{experienceId}/availability` | ExperienceProvider; owner enforced | Path: `experienceId`; body `CreateExperienceAvailabilityRequestDto` | `201 ExperienceAvailabilityResponseDto` |
| `PATCH /api/experience-availability/{availabilityId}/activate` | ExperienceProvider; owner enforced | Path: `availabilityId` | `200 ExperienceAvailabilityResponseDto` |
| `PATCH /api/experience-availability/{availabilityId}/deactivate` | ExperienceProvider; owner enforced | Path: `availabilityId` | `200 ExperienceAvailabilityResponseDto` |

### Bookings

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `POST /api/experiences/{experienceId}/bookings` | Traveler | Path: `experienceId`; body `CreateExperienceBookingRequestDto` | `200 ExperienceBookingResponseDto` |
| `GET /api/experience-bookings/my` | Traveler | None | `200 ExperienceBookingResponseDto[]` |
| `GET /api/experiences/{experienceId}/bookings` | ExperienceProvider; owner enforced | Path: `experienceId` | `200 ExperienceBookingResponseDto[]` |
| `PATCH /api/experience-bookings/{bookingId}/cancel` | Traveler; owner enforced | Path: `bookingId` | `200 ExperienceBookingResponseDto` |
| `PATCH /api/experience-bookings/{bookingId}/complete` | ExperienceProvider; owner enforced | Path: `bookingId` | `200 ExperienceBookingResponseDto` |

### Reviews

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/experiences/{experienceId}/reviews` | Public | Path: `experienceId` | `200 ExperienceReviewResponseDto[]` |
| `POST /api/experiences/{experienceId}/reviews` | Traveler | Path: `experienceId`; body `CreateExperienceReviewRequestDto` | `201 ExperienceReviewResponseDto` |
| `PUT /api/experience-reviews/{reviewId}` | Traveler; owner enforced | Path: `reviewId`; body `UpdateExperienceReviewRequestDto` | `200 ExperienceReviewResponseDto` |
| `DELETE /api/experience-reviews/{reviewId}` | Traveler; owner enforced | Path: `reviewId` | `204 No Content` |

### Admin moderation

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/admin/experiences` | Admin | Query: `approvalStatus?`, `isActive?` | `200 ExperienceSummaryDto[]` |
| `PATCH /api/admin/experiences/{id}/approval-status` | Admin | Path: `id`; body `SetExperienceApprovalStatusRequestDto` | `200 ExperienceResponseDto` |
| `GET /api/admin/experiences/{id}/moderation-history` | Admin | Query: `page`, `pageSize` | `200 ExperienceModerationEventDto[]` |

### Request bodies

```ts
interface CreateExperienceRequestDto {
  categoryId: string;
  adm3Gid: number;         // required; existing Regions neighbourhood gid
  title: string;           // required, max 200
  description: string;     // required, max 3000
  locationName: string;    // required, max 300
  pricePerPerson: number;  // > 0
  currency: string;        // required, max 10; defaults to EGP
  durationMinutes: number; // > 0
  maxGuests: number;       // > 0
  latitude: number;        // -90..90
  longitude: number;       // -180..180
  vibeIds: string[];
  tags: string[];
}

type UpdateExperienceRequestDto = CreateExperienceRequestDto;

interface CreateExperienceAvailabilityRequestDto {
  startTimeUtc: string; // future ISO UTC timestamp
  endTimeUtc: string;   // after startTimeUtc
  capacity: number;     // > 0
}

interface CreateExperienceBookingRequestDto {
  availabilityId: string;
  guestsCount: number; // > 0, within experience and slot capacity
}

interface CreateExperienceReviewRequestDto {
  rating: number; // integer 1..5
  comment?: string | null; // max 2000
}

type UpdateExperienceReviewRequestDto = CreateExperienceReviewRequestDto;

interface SetExperienceApprovalStatusRequestDto {
  approvalStatus: ExperienceApprovalStatus;
  moderationNotes?: string | null; // max 1000
}
```

Rejected Experience decisions require moderation notes. Creation, Admin
decisions, and provider updates that reset approval to Pending are recorded in
append-only moderation history.

Availability slots cannot overlap another active slot. Experiences must be active and approved before booking. A traveler cannot duplicate an active booking for the same slot. Reviews require a completed booking and are limited to one review per traveler/experience.

### Response shapes

```ts
interface ExperienceCategoryResponseDto { id: string; name: string; description: string | null; }
interface VibeResponseDto { id: string; name: string; }
interface ExperienceTagDto { id: string; experienceId: string; name: string; }

interface ExperienceSummaryDto {
  id: string;
  providerProfileId: string;
  categoryId: string;
  categoryName: string | null;
  adm3Gid: number;
  region: RegionReferenceDto | null;
  title: string;
  locationName: string;
  pricePerPerson: number;
  currency: string;
  durationMinutes: number;
  maxGuests: number;
  isActive: boolean;
  approvalStatus: ExperienceApprovalStatus;
  moderationNotes: string | null;
  vibes: string[];
  tags: string[];
}

interface ExperienceResponseDto {
  id: string;
  providerProfileId: string;
  categoryId: string;
  categoryName: string | null;
  adm3Gid: number;
  region: RegionReferenceDto | null;
  title: string;
  description: string;
  locationName: string;
  pricePerPerson: number;
  currency: string;
  durationMinutes: number;
  maxGuests: number;
  latitude: number;
  longitude: number;
  isActive: boolean;
  approvalStatus: ExperienceApprovalStatus;
  moderationNotes: string | null;
  moderatedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  vibes: VibeResponseDto[];
  tags: ExperienceTagDto[];
}

interface ExperienceAvailabilityResponseDto {
  id: string;
  experienceId: string;
  startTimeUtc: string;
  endTimeUtc: string;
  capacity: number;
  bookedCount: number;
  remainingCapacity: number;
  isActive: boolean;
  createdAtUtc: string;
}

interface ExperienceBookingResponseDto {
  id: string;
  experienceId: string;
  availabilityId: string;
  travelerProfileId: string;
  guestsCount: number;
  totalPrice: number;
  status: ExperienceBookingStatus;
  createdAtUtc: string;
  cancelledAtUtc: string | null;
  completedAtUtc: string | null;
}

interface ExperienceReviewResponseDto {
  id: string;
  experienceId: string;
  travelerProfileId: string;
  rating: number;
  comment: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}
```

## 8. Regions module

All regular region read and CRUD endpoints are public in the current implementation. The write endpoints should be admin-protected before production.

### Frontend location-selection flow

1. Load countries, then governorates, districts, and neighbourhoods through the parent-scoped endpoints below.
2. Store/send the selected neighbourhood's numeric `gid` as `adm3Gid` when creating a stay or experience.
3. Alternatively, call `GET /api/regions/by-point?lat=...&lon=...` after map selection and use its `adm3Gid`.
4. Display the `region` object returned with stay/experience responses; no extra hierarchy request is required.

### Shared query parameters

List endpoints accept:

| Parameter | Type | Meaning |
|---|---|---|
| `search` | string? | Partial English/Arabic name or pcode match |
| `geometryAccuracy` | integer, 0..100 | `0` omits geometry; `100` full geometry; `1..99` simplified geometry |
| `page` | integer, default 1 | Page number |
| `pageSize` | integer, default 20 | Page size |

Single-item GET endpoints accept optional `geometryAccuracy=0..100`.

### Lookup and country endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/regions/by-point` | Public | Query: `lat` (-90..90), `lon` (-180..180); always send both | `200 RegionHierarchyGidsDto` |
| `GET /api/regions/countries` | Public | Shared list query | `200 Adm0Dto[]` |
| `GET /api/regions/countries/{gid}` | Public | Path: integer `gid`; query `geometryAccuracy?` | `200 Adm0Dto` |
| `POST /api/regions/countries` | Public ⚠ | `CreateAdm0Request` body | `201 Adm0Dto` |
| `PUT /api/regions/countries/{gid}` | Public ⚠ | Path: `gid`; `UpdateAdm0Request` body | `200 Adm0Dto` |
| `DELETE /api/regions/countries/{gid}` | Public ⚠ | Path: `gid` | `204 No Content` |

### Governorate endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/regions/governorates` | Public | Shared list query | `200 Adm1Dto[]` |
| `GET /api/regions/governorates/{gid}` | Public | Path: `gid`; `geometryAccuracy?` | `200 Adm1Dto` |
| `GET /api/regions/countries/{adm0Gid}/governorates` | Public | Path: `adm0Gid`; shared list query | `200 Adm1Dto[]` |
| `POST /api/regions/governorates` | Public ⚠ | `CreateAdm1Request` body | `201 Adm1Dto` |
| `PUT /api/regions/governorates/{gid}` | Public ⚠ | Path: `gid`; `UpdateAdm1Request` body | `200 Adm1Dto` |
| `DELETE /api/regions/governorates/{gid}` | Public ⚠ | Path: `gid` | `204 No Content` |

### District endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/regions/districts` | Public | Shared list query | `200 Adm2Dto[]` |
| `GET /api/regions/districts/{gid}` | Public | Path: `gid`; `geometryAccuracy?` | `200 Adm2Dto` |
| `GET /api/regions/governorates/{adm1Gid}/districts` | Public | Path: `adm1Gid`; shared list query | `200 Adm2Dto[]` |
| `POST /api/regions/districts` | Public ⚠ | `CreateAdm2Request` body | `201 Adm2Dto` |
| `PUT /api/regions/districts/{gid}` | Public ⚠ | Path: `gid`; `UpdateAdm2Request` body | `200 Adm2Dto` |
| `DELETE /api/regions/districts/{gid}` | Public ⚠ | Path: `gid` | `204 No Content` |

### Neighbourhood endpoints

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `GET /api/regions/neighbourhoods` | Public | Shared list query | `200 Adm3Dto[]` |
| `GET /api/regions/neighbourhoods/{gid}` | Public | Path: `gid`; `geometryAccuracy?` | `200 Adm3Dto` |
| `GET /api/regions/districts/{adm2Gid}/neighbourhoods` | Public | Path: `adm2Gid`; shared list query | `200 Adm3Dto[]` |
| `POST /api/regions/neighbourhoods` | Public ⚠ | `CreateAdm3Request` body | `201 Adm3Dto` |
| `PUT /api/regions/neighbourhoods/{gid}` | Public ⚠ | Path: `gid`; `UpdateAdm3Request` body | `200 Adm3Dto` |
| `DELETE /api/regions/neighbourhoods/{gid}` | Public ⚠ | Path: `gid` | `204 No Content` |

### Admin GeoJSON imports

| Method and path | Auth | Input | Success response |
|---|---|---|---|
| `POST /api/admin/regions/import/adm0` | Admin | `multipart/form-data`, field `file`, `.geojson` only | `200 GeoJsonImportResultDto`; `207` on partial failure |
| `POST /api/admin/regions/import/adm1` | Admin | Same; ADM0 must exist first | `200/207 GeoJsonImportResultDto` |
| `POST /api/admin/regions/import/adm2` | Admin | Same; ADM1 must exist first | `200/207 GeoJsonImportResultDto` |
| `POST /api/admin/regions/import/adm3` | Admin | Same; ADM2 must exist first | `200/207 GeoJsonImportResultDto` |
| `POST /api/admin/regions/import/all-local` | Admin | No body; development utility | `200/207 GeoJsonImportResultDto[]` |

### Region request bodies

```ts
interface CreateAdm0Request {
  nameEn: string;
  nameAr?: string | null;
  pcode: string;
  imageUrl?: string | null;
  flagUrl?: string | null;
}
interface UpdateAdm0Request {
  nameEn: string;
  nameAr?: string | null;
  imageUrl?: string | null;
  flagUrl?: string | null;
}

interface CreateAdm1Request {
  adm0Gid: number;
  nameEn: string;
  nameAr?: string | null;
  pcode: string;
  imageUrl?: string | null;
}
interface UpdateAdm1Request {
  nameEn: string;
  nameAr?: string | null;
  imageUrl?: string | null;
}

interface CreateAdm2Request {
  adm1Gid: number;
  nameEn: string;
  nameAr?: string | null;
  pcode: string;
  imageUrl?: string | null;
}
type UpdateAdm2Request = UpdateAdm1Request;

interface CreateAdm3Request {
  adm2Gid: number;
  nameEn: string;
  nameAr?: string | null;
  pcode: string;
  imageUrl?: string | null;
}
type UpdateAdm3Request = UpdateAdm1Request;
```

### Region response shapes

```ts
interface Adm0Dto {
  gid: number;
  nameEn: string;
  nameAr: string | null;
  pcode: string;
  imageUrl: string | null;
  flagUrl: string | null;
  createdAt: string;
  updatedAt: string;
  geometryGeoJson: object | null;
}

interface Adm1Dto {
  gid: number;
  adm0Gid: number;
  nameEn: string;
  nameAr: string | null;
  pcode: string;
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string;
  geometryGeoJson: object | null;
}

interface Adm2Dto {
  gid: number;
  adm1Gid: number;
  nameEn: string;
  nameAr: string | null;
  pcode: string;
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string;
  geometryGeoJson: object | null;
}

interface Adm3Dto {
  gid: number;
  adm2Gid: number;
  nameEn: string | null;
  nameAr: string | null;
  pcode: string;
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string;
  geometryGeoJson: object | null;
}

interface RegionHierarchyGidsDto {
  adm0Gid: number | null;
  adm1Gid: number | null;
  adm2Gid: number | null;
  adm3Gid: number | null;
}

interface RegionReferenceDto {
  adm3Gid: number;
  neighbourhoodNameEn: string | null;
  neighbourhoodNameAr: string | null;
  adm2Gid: number;
  districtNameEn: string;
  districtNameAr: string | null;
  adm1Gid: number;
  governorateNameEn: string;
  governorateNameAr: string | null;
  adm0Gid: number;
  countryNameEn: string;
  countryNameAr: string | null;
}

interface GeoJsonImportResultDto {
  success: boolean;
  layer: string;
  totalFeatures: number;
  inserted: number;
  updated: number;
  skipped: number;
  errors: string[];
}
```

## 9. Communication module

### REST endpoints

| Method and path | Auth | Behavior |
|---|---|---|
| `GET /api/chat/threads` | Bearer | Lists the current user's threads |
| `POST /api/chat/threads/direct` | Bearer | Creates or returns a direct thread |
| `GET /api/chat/threads/{threadId}/messages` | Bearer + participant | Lists messages |
| `POST /api/chat/threads/{threadId}/messages` | Bearer + participant | Persists and broadcasts a message |
| `PATCH /api/chat/threads/{threadId}/read` | Bearer + participant | Marks read and broadcasts `ThreadRead` |
| `GET /api/notifications` | Bearer | Lists in-app notifications |
| `PATCH /api/notifications/{id}/read` | Bearer | Marks one notification read |
| `PATCH /api/notifications/read-all` | Bearer | Marks all notifications read |
| `GET /api/notifications/preferences` | Bearer | Returns saved or default preferences |
| `PUT /api/notifications/preferences` | Bearer | Replaces the user's preferences |

### SignalR

Connect to `/hubs/chat` with the access JWT. Browser WebSocket clients may pass
it as the standard `access_token` query value. Call `JoinThread(threadId)`
before listening; nonparticipants are rejected. `LeaveThread(threadId)` removes
the subscription.

Server-to-client events:

```ts
interface ChatMessageEvent {
  id: string;
  threadId: string;
  senderUserId: string;
  body: string;
  sentAtUtc: string;
}

interface ChatThreadReadEvent {
  threadId: string;
  userId: string;
  readAtUtc: string;
}
```

- `MessageReceived`: `ChatMessageEvent`
- `ThreadRead`: `ChatThreadReadEvent`

Messages are sent through the REST endpoint so existing validation and rate
limits remain authoritative.

### Notification preferences

```ts
interface NotificationPreference {
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  chatMessageNotificationsEnabled: boolean;
  systemNotificationsEnabled: boolean;
  updatedAtUtc?: string | null;
}
```

Email and push selections are persisted, but delivery providers are not yet
implemented. In-app and category switches immediately control in-app creation.

## 10. Dashboards and analytics

| Method and path | Auth | Behavior |
|---|---|---|
| `GET /api/dashboard/admin` | Admin | Platform totals, moderation queues, bookings, and communication activity |
| `GET /api/dashboard/hotel-owner` | HotelOwner | Owned stays, booking status, upcoming arrivals, reviews, and revenue |
| `GET /api/dashboard/experience-provider` | ExperienceProvider | Owned Experiences, approval status, capacity, bookings, reviews, and revenue |
| `GET /api/admin/analytics` | Admin | Date-window daily business activity and currency-grouped revenue |
| `GET /metrics` | Admin | Prometheus operational metrics; not intended for frontend use |

Analytics accepts optional `from` and `to` query dates in `YYYY-MM-DD` format.
The default is the latest 30 UTC calendar days and the maximum range is 366
days. Missing dates in the range are returned with zero counts. Revenue is
always grouped by currency.

Provider dashboard data is scoped from the authenticated user's provider
profile; clients do not send a profile ID.

## 11. System endpoints

| Method and path | Auth | Behavior |
|---|---|---|
| `GET /` | Public | Returns `302` and redirects to `/swagger` |
| `GET /health/live` | Public | Process liveness |
| `GET /health/ready` | Public | PostgreSQL connectivity and migration readiness |

## 12. Recommended frontend integration order

1. Frontend creates a shared API client with bearer and refresh-token handling.
2. Integrate registration, email confirmation, password reset, and login.
3. Integrate Admin MFA setup/verification and role-aware routing.
4. Integrate role-specific profile creation because stays/experiences/bookings depend on profile records.
5. Integrate public lookup data: interests, local buddies, categories, vibes, and regions.
6. Integrate stays and experiences listing/detail screens.
7. Integrate Traveler booking/review flows.
8. Integrate HotelOwner and ExperienceProvider management flows.
9. Integrate Admin user, verification, moderation, and region import screens.
10. Add SignalR thread subscriptions and notification preference settings.
11. Add role-specific dashboards and Admin analytics.

## 13. Current contract caveats

- There is no API version prefix such as `/api/v1`.
- There is no backend file upload for profile images; the client sends an existing HTTP/HTTPS URL.
- Failed requests use Problem Details with `errorCode` and `traceId`.
- Swagger response metadata is incomplete for methods returning `IActionResult`; the response types in this guide come from controller handlers and DTOs.
- The integration suite covers security-critical module workflows.
