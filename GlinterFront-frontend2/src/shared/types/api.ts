// ============================================================
// Profiles Module DTOs (matching backend)
// ============================================================

export interface InterestResponse {
  id: string;
  name: string;
}

export interface TravelerProfileResponse {
  profileId: string;
  userId: string;
  profileType: "Traveler";
  displayName: string;
  bio?: string;
  nationality?: string;
  preferredBudgetLevel?: string;
  travelStyle?: string;
  preferredInterests?: string;
  profileImageUrl?: string;
  interests: InterestResponse[];
  followersCount: number;
  followingCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface LocalBuddyListItemResponse {
  profileId: string;
  userId: string;
  displayName: string;
  bio?: string;
  city: string;
  languages?: string;
  profileImageUrl?: string;
  rating: number;
  reviewsCount: number;
  verificationStatus: string;
  interests: InterestResponse[];
  followersCount: number;
  followingCount: number;
}

export interface LocalBuddyProfileResponse {
  profileId: string;
  userId: string;
  profileType: "LocalBuddy";
  displayName: string;
  bio?: string;
  city: string;
  languages?: string;
  profileImageUrl?: string;
  rating: number;
  reviewsCount: number;
  verificationStatus: string;
  interests: InterestResponse[];
  createdAtUtc: string;
  updatedAtUtc?: string;
  followersCount: number;
  followingCount: number;
}

export interface TravelerProfileRequest {
  displayName: string;
  bio?: string;
  nationality?: string;
  preferredBudgetLevel?: string;
  travelStyle?: string;
  preferredInterests?: string;
  interestIds: string[];
}

// ============================================================
// Experiences Module DTOs (matching backend)
// ============================================================

export type ExperienceCategory = "Historical" | "Nature" | "Shopping" | "Nightlife" | "Dining";
export type ExperienceSourceType = "ThirdParty" | "Provider";
export type ExperienceBookingStatus = "Pending" | "Confirmed" | "Completed" | "Cancelled";
export type ExperienceModerationStatus = "Pending" | "Approved" | "Rejected";

export interface ExperienceImageDto {
  id: number;
  link: string;
}

export interface ExperienceHourDto {
  id: number;
  dayOfWeek: string;
  opensAt: string;
  closesAt: string;
}

export interface ExperienceReviewDto {
  id: number;
  externalReviewId?: string;
  reviewerName?: string;
  rating?: number;
  reviewText?: string;
  publishedAtDate?: string;
  sourceList: string;
}

export interface ExperienceResponseDto {
  id: number;
  category: ExperienceCategory;
  sourceType: ExperienceSourceType;
  createdByUserId?: string;
  providerProfileId?: string;
  name: string;
  description?: string;
  address?: string;
  cid?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  latitude?: number;
  longitude?: number;
  featuredImages: ExperienceImageDto[];
  hours: ExperienceHourDto[];
  googleMapsLink?: string;
  popularTimes: Array<{
    id: number;
    dayOfWeek: string;
    hourOfDay: number;
    popularityPercentage: number;
  }>;
  phoneInternational?: string;
  priceRange?: string;
  reviews?: number;
  rating?: number;
  reviewsPerRating: Array<{ rating: number; reviewsCount: number }>;
  website?: string;
  amenities: string[];
  featuredReviews: ExperienceReviewDto[];
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
  currentInsight?: {
    requestedAt: string;
    dayOfWeek: string;
    hourOfDay: number;
    isOpen?: boolean;
    openStatus: string;
    popularityPercentage?: number;
    crowdLevel: string;
    bestKnownOpenWindow?: string;
  };
  moderationStatus: ExperienceModerationStatus;
  moderationNotes?: string;
  moderatedByUserId?: string;
  moderatedAtUtc?: string;
}

export type ExperienceSummaryDto = ExperienceResponseDto;

export interface CreateExperienceRequest {
  category: ExperienceCategory;
  name: string;
  description?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  featuredImageLinks: string[];
  hours: Array<{ dayOfWeek: string; opensAt: string; closesAt: string }>;
  googleMapsLink?: string;
  popularTimes: Array<{ dayOfWeek: string; hourOfDay: number; popularityPercentage: number }>;
  phoneInternational?: string;
  priceRange?: string;
  website?: string;
  amenities: string[];
}

export type UpdateExperienceRequest = CreateExperienceRequest;

export interface ExperienceAvailabilityDto {
  id: string;
  experienceId: number;
  startTimeUtc: string;
  endTimeUtc: string;
  capacity: number;
  remainingCapacity: number;
  pricePerPerson: number;
  isActive: boolean;
}

export interface CreateExperienceAvailabilityRequest {
  startTimeUtc: string;
  endTimeUtc: string;
  capacity: number;
  pricePerPerson: number;
}

export interface ExperienceBookingResponseDto {
  id: string;
  experienceId: number;
  experienceName: string;
  availabilityId: string;
  travelerProfileId: string;
  travelerName: string;
  startTimeUtc: string;
  endTimeUtc: string;
  guestsCount: number;
  totalPrice: number;
  status: ExperienceBookingStatus;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateExperienceBookingRequest {
  availabilityId: string;
  guestsCount: number;
}

export interface GetExperiencesRequest {
  category?: ExperienceCategory;
  sourceType?: ExperienceSourceType;
  search?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  minRating?: number;
  page?: number;
  pageSize?: number;
}

export interface CreateExperienceReviewRequest {
  rating: number;
  reviewText: string;
}

// ============================================================
// Stays Module DTOs (matching backend)
// ============================================================

export type StaySourceType = "ThirdParty" | "HotelOwner";
export type StayBookingStatus = "Pending" | "Confirmed" | "Completed" | "Cancelled";

export interface StayImageDto {
  id: number;
  link: string;
}

export interface StayBookingPlatformDto {
  id: number;
  name: string;
  priceWithTax?: number;
  link?: string;
}

export interface StayReviewDto {
  id: number;
  externalReviewId?: string;
  reviewerName?: string;
  rating?: number;
  reviewText?: string;
  platform: string;
  publishedAtDate?: string;
}

export interface StayResponseDto {
  id: number;
  sourceType: StaySourceType;
  createdByUserId?: string;
  hotelOwnerProfileId?: string;
  name: string;
  price?: number;
  description?: string;
  googleMapsLink?: string;
  reviews?: number;
  rating?: number;
  website?: string;
  phoneInternational?: string;
  locationSummaryDescription?: string;
  cid?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
  amenities: string[];
  images: StayImageDto[];
  reviewsPerRating: { rating: number; reviewsCount: number }[];
  bookingPlatforms: StayBookingPlatformDto[];
  featuredReviews: StayReviewDto[];
}

export interface CreateStayRequest {
  name: string;
  price: number;
  description?: string;
  googleMapsLink?: string;
  website?: string;
  phoneInternational?: string;
  locationSummaryDescription?: string;
  latitude?: number;
  longitude?: number;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  imageLinks: string[];
  amenities: string[];
  bookingPlatforms: Array<{
    name: string;
    priceWithTax?: number;
    link?: string;
  }>;
}

export type UpdateStayRequest = CreateStayRequest;

export interface GetStaysRequest {
  sourceType?: StaySourceType;
  search?: string;
  adm0Gid?: number;
  adm1Gid?: number;
  adm2Gid?: number;
  adm3Gid?: number;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  sortBy?: "Recommended" | "Price" | "Rating" | "Reviews" | "Name" | "Newest";
  sortDirection?: "Asc" | "Desc";
  page?: number;
  pageSize?: number;
}

export interface PagedResponse<T> {
  page: number;
  pageSize: number;
  totalCount: number;
  items: T[];
}

export interface StayBookingResponseDto {
  id: string;
  stayId: number;
  stayName: string;
  travelerProfileId: string;
  guestName: string;
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
  totalPrice: number;
  status: StayBookingStatus;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface CreateStayBookingRequest {
  checkInDate: string;
  checkOutDate: string;
  guestCount: number;
}

export interface CreateStayReviewRequest {
  rating: number;
  reviewText: string;
}

// ============================================================
// Admin Module DTOs (matching backend)
// ============================================================

export interface AdminUserListItem {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  role: string;
}

export interface AdminUserResponse {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAtUtc: string;
  roles: string[];
}

export interface AdminRoleResponse {
  name: string;
}

export interface AdminAssignRoleRequest {
  role: string;
}

export interface AdminChangeUserStatusRequest {
  isActive: boolean;
}

export interface AdminGetExperiencesRequest {
  moderationStatus?: ExperienceModerationStatus;
  isActive?: boolean;
}

export interface AdminModerateExperienceRequest {
  moderationStatus: ExperienceModerationStatus;
  moderationNotes?: string;
}

export interface AdminUpdateBuddyVerificationRequest {
  verificationStatus: string;
  moderationNotes?: string;
}

export interface AdminLocalBuddy {
  userId: string;
  profileId: string;
  displayName: string;
  city: string;
  languages?: string;
  profileImageUrl?: string;
  rating: number;
  reviewsCount: number;
  verificationStatus: "Pending" | "Approved" | "Rejected";
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface AdminDashboard {
  totalUsers: number;
  activeUsers: number;
  pendingBuddyVerifications: number;
  approvedBuddies: number;
  pendingExperiences: number;
  approvedExperiences: number;
  activeStays: number;
  totalBookings: number;
  totalBookingValue: number;
  auditEventsLast24Hours: number;
}

export interface AdminAnalytics {
  usersByRole: Record<string, number>;
  bookingsByStatus: Record<string, number>;
  listingsByType: Record<string, number>;
  stayBookingValue: number;
  experienceBookingValue: number;
}

export interface AdminAuditEvent {
  id: string;
  actorUserId: string;
  action: string;
  httpMethod: string;
  path: string;
  target?: string;
  statusCode: number;
  succeeded: boolean;
  correlationId: string;
  createdAtUtc: string;
  completedAtUtc?: string;
  changes?: unknown;
}

export interface AdminAuditPage {
  items: AdminAuditEvent[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// ============================================================
// Communication Module DTOs
// ============================================================

export interface ChatThreadDto {
  id: string;
  type: "Direct" | "Group";
  title?: string;
  participantUserIds: string[];
  participantDisplayNames: Record<string, string>;
  lastMessageBody?: string;
  lastMessageSenderUserId?: string;
  lastMessageAtUtc?: string;
  unreadCount: number;
  createdAtUtc: string;
}

export interface ChatMessageDto {
  id: string;
  threadId: string;
  senderUserId: string;
  body: string;
  sentAtUtc: string;
  isMine: boolean;
}

export interface ChatMessageEventDto {
  id: string;
  threadId: string;
  senderUserId: string;
  body: string;
  sentAtUtc: string;
}

export interface ChatThreadReadEventDto {
  threadId: string;
  userId: string;
  readAtUtc: string;
}

export interface NotificationDto {
  id: string;
  type: "System" | "ChatMessage" | "Booking" | "Moderation" | "Support";
  title: string;
  body: string;
  linkUrl?: string;
  sourceModule?: string;
  sourceEntityType?: string;
  sourceEntityId?: string;
  createdAtUtc: string;
  readAtUtc?: string;
  isRead: boolean;
}

export interface NotificationsPageDto {
  items: NotificationDto[];
  unreadCount: number;
  page: number;
  pageSize: number;
}

export interface NotificationPreferencesDto {
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  chatMessageNotificationsEnabled: boolean;
  systemNotificationsEnabled: boolean;
  updatedAtUtc?: string;
}

export type UpdateNotificationPreferencesRequest = Omit<
  NotificationPreferencesDto,
  "updatedAtUtc"
>;
